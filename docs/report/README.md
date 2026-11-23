# Report

`report.md` is the IEEE-structured report (Abstract, I-XII). Open items are marked **[TODO live]** (numbers from the live run with the local LLM,
two-instance test results, demo video link), plus the author line and the instructor approval note at the top.

Export to PDF (A4, two columns, needs pandoc and a LaTeX engine):

    pandoc docs/report/report.md -o report.pdf --pdf-engine=xelatex -V geometry:a4paper -V classoption=twocolumn -V fontsize=10pt

Or paste the Markdown into Word/Google Docs and apply the IEEE conference template. Add the screenshots listed in `docs/demo-script.md`
(AI panel with tool log, decision log, MCP Inspector, fog-of-war comparison) as figures.
