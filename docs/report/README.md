# Report

`report.md` is the IEEE-structured report (Abstract, I-XII, references). All live numbers are filled in (26 Nov 2026). No slides and no video (student, 26 Nov 2026): the presentation is the live demo.

`report.docx` and `report.pdf` are built from `report.md` (IEEE conference layout, A4, title block in one column, body in two
columns, Times New Roman 10 pt). Fig. 1 is `fig1_architecture.png`. To build them again after a change, from the repository root:

    cd tools/report && npm install docx@9.5.1 && cd ../..         # once
    python tools/report/figure.py docs/report/fig1_architecture.png
    node tools/report/build.js docs/report/report.md docs/report/fig1_architecture.png docs/report/report.docx
    powershell -ExecutionPolicy Bypass -File tools/report/to_pdf.ps1  # needs Microsoft Word
