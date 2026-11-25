"""Compares what the host and the client of the automatic two-player match saw (tools/run_auto_match.ps1).

python evaluation/two_players_report.py HOST.txt CLIENT.txt  ->  evaluation/unity_two_players.md
Exit code 1 when a check failed or the two sides disagree.
"""
import sys
from bisect import bisect_left
from datetime import datetime
from pathlib import Path

NEAR = 32        # ticks (0.5 s at 64 Hz): the client shows other objects a little later than the host
POSITION = 1.5   # metres: a Villager walks 1.75 m in half a second


def load(path):
    rows, errors, objects, states = [], [], {}, {}
    for line in Path(path).read_text(encoding="utf-8").splitlines():
        p = line.split(";")
        if p[0] == "ROW":
            rows.append(p[1:])
        elif p[0] == "E":
            errors.append(p[1] + ": " + ";".join(p[2:]))
        elif p[0] == "S":
            objects.setdefault(int(p[1]), {})[p[2]] = (p[3], float(p[4]), float(p[5]), float(p[6]))
        elif p[0] == "P" and p[2] != "0":
            states.setdefault(int(p[1]), {})[p[2]] = tuple(p[3:])
    return rows, errors, objects, states


def compare(host, client):
    """Every client sample against the host samples around the same tick."""
    ticks, samples, values, bad, worst = sorted(host), 0, 0, [], 0.0
    for t in sorted(client):
        i = bisect_left(ticks, t - NEAR)
        around = [host[k] for k in ticks[i:] if k <= t + NEAR]
        if len(around) < 2:
            continue
        samples += 1
        for oid in set(client[t]) | set.union(*[set(h) for h in around]):
            values += 1
            mine, theirs = client[t].get(oid), [h[oid] for h in around if oid in h]
            if mine is None:
                if len(theirs) == len(around):   # there the whole time on the host
                    bad.append(f"tick {t}: object {oid} ({theirs[0][0]}) is missing on the client")
            elif not theirs:
                bad.append(f"tick {t}: object {oid} ({mine[0]}) exists only on the client")
            elif isinstance(mine[1], float):
                d = min(((mine[1] - h[1]) ** 2 + (mine[2] - h[2]) ** 2) ** 0.5 for h in theirs)
                worst = max(worst, d)
                if d > POSITION:
                    bad.append(f"tick {t}: {mine[0]} {oid} is {d:.1f} m away from the host's position")
                # destroyed on the host in this half second: several hits may lie between its last sample and the end (HP 0)
                low = min(h[3] for h in theirs) if len(theirs) == len(around) else 0.0
                if not low - 0.01 <= mine[3] <= max(h[3] for h in theirs) + 0.01:
                    bad.append(f"tick {t}: {mine[0]} {oid} HP {mine[3]} on the client, {theirs[0][3]} on the host")
            elif mine not in theirs:
                bad.append(f"tick {t}: player {oid} {mine} on the client, {theirs[0]} on the host")
    return samples, values, worst, bad


def main(host_file, client_file):
    h_rows, h_err, h_obj, h_state = load(host_file)
    c_rows, c_err, c_obj, c_state = load(client_file)
    n, values, worst, bad = compare(h_obj, c_obj)
    sn, svalues, _, sbad = compare(h_state, c_state)
    rows = [("host (P1)", *r) for r in h_rows] + [("client (P2)", *r) for r in c_rows]
    failed = [r for r in rows if r[3] != "ok"]
    out = [f"# Automatic two-player match ({datetime.now():%Y-%m-%d %H:%M})", "",
           "Two instances of the Windows test build (Fusion host and client through Photon, no window). The client plays with a virtual mouse "
           "through the normal scripts. `Testing/AutoMatch.cs` is not in the game build.", "",
           f"**{len(rows) - len(failed)} passed, {len(failed)} failed.**", "",
           f"Same state on both screens: {n} client samples with {values} object values (position within {POSITION} m, HP, existence): "
           f"**{len(bad)} differences**, largest distance {worst:.2f} m; {sn} samples with {svalues} player values (resources, population, age, "
           f"result): **{len(sbad)} differences**.", "",
           "| Side | Section | Check | Result | Detail |", "|---|---|---|---|---|"]
    out += ["| " + " | ".join(c.replace("|", "/") for c in (r[0], r[1], r[2], "ok" if r[3] == "ok" else "**FAIL**", ";".join(r[4:]))) + " |" for r in rows]
    out += ["", f"Errors in the logs: host {len(h_err)}, client {len(c_err)}", ""]
    out += [f"- {side}: {e}" for side, es in (("host", h_err), ("client", c_err)) for e in es[:15]]
    out += [f"- difference: {b}" for b in (bad + sbad)[:20]]
    target = Path(__file__).with_name("unity_two_players.md")
    target.write_text("\n".join(out).rstrip() + "\n", encoding="utf-8")
    print("\n".join(out[4:7]), f"\nfailed: {failed}\n-> {target}")
    return 1 if failed or bad or sbad else 0


if __name__ == "__main__":
    sys.exit(main(*sys.argv[1:3]))
