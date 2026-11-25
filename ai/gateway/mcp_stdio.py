"""Run the MCP server as a real subprocess and talk to it over stdio (the MCP protocol), instead of in-process.

StdioMcp has the same two methods the Agent uses on the in-process server (list_tools, call_tool), so the Agent code is
unchanged. The MCP client session lives in its own event-loop thread, because the Agent calls asyncio.run() for every call.
Use as a context manager:  with StdioMcp(session_id, db_path) as mcp: Agent(llm, mcp, ...).run(...)
The gateway keeps one open per game session (open/close): starting one per question cost 26 s at the first knowledge search.
"""
import asyncio
import os
import sys
import threading
from pathlib import Path

from mcp import ClientSession, StdioServerParameters
from mcp.client.stdio import stdio_client

SERVER = Path(__file__).resolve().parents[1] / "mcp-server" / "server.py"


class StdioMcp:
    def __init__(self, session_id, db_path, timeout_s=60):
        self.session_id, self.db_path, self.timeout_s = session_id, str(db_path), timeout_s
        self._loop = asyncio.new_event_loop()
        self._thread = threading.Thread(target=self._loop.run_forever, daemon=True)
        self._stop = None
        self._session = None

    # ---- lifecycle ----
    def __enter__(self):
        self._thread.start()
        ready = asyncio.run_coroutine_threadsafe(self._start(), self._loop)
        ready.result(timeout=self.timeout_s * 3)
        return self

    def __exit__(self, *exc):
        if self._stop is not None:
            self._loop.call_soon_threadsafe(self._stop.set)
        self._thread.join(timeout=5)
        return False

    def open(self):
        return self.__enter__()

    def close(self):
        self.__exit__()

    async def _start(self):
        self._stop = asyncio.Event()
        self._ready = asyncio.Event()
        self._runner = asyncio.create_task(self._hold_session())
        await asyncio.wait_for(self._ready.wait(), self.timeout_s * 3)
        if self._session is None:
            raise RuntimeError("MCP server subprocess did not start")

    async def _hold_session(self):
        # The server gets the session id from its environment, never from tool arguments.
        env = dict(os.environ, RTS_SESSION_ID=self.session_id, SESSION_DB_PATH=self.db_path)
        params = StdioServerParameters(command=sys.executable, args=[str(SERVER)], env=env)
        try:
            async with stdio_client(params) as (read, write):
                async with ClientSession(read, write) as session:
                    await session.initialize()
                    self._session = session
                    self._ready.set()
                    await self._stop.wait()
        finally:
            self._ready.set()

    # ---- the two calls used by the Agent (awaitable from any event loop) ----
    async def list_tools(self):
        res = await self._submit(self._session.list_tools())
        return res.tools

    async def call_tool(self, name, args):
        return await self._submit(self._session.call_tool(name, args or {}))

    async def _submit(self, coro):
        fut = asyncio.run_coroutine_threadsafe(asyncio.wait_for(coro, self.timeout_s), self._loop)
        return await asyncio.wrap_future(fut)
