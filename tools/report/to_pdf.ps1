# Word -> PDF with the installed Microsoft Word (A4 layout as in the .docx).
# Run from the repository root: powershell -ExecutionPolicy Bypass -File tools/report/to_pdf.ps1
$root = (Resolve-Path "$PSScriptRoot\..\..").Path
$w = New-Object -ComObject Word.Application
$w.Visible = $false; $w.DisplayAlerts = 0
try {
    $d = $w.Documents.Open("$root\docs\report\report.docx", $false, $true)
    $d.SaveAs2("$root\docs\report\report.pdf", 17)
    "pages: " + $d.ComputeStatistics(2)
    $d.Close(0)
} finally { $w.Quit() }
