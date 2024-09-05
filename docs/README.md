# Getting Started

### Install Core Dependancies

```shell
.\Titanium.exe install --lang eng
.\Titanium.exe config --key OpenAIApiKey --value _ENTER_KEY_HERE_
```

### Create a New Project

```shell
.\Titanium.exe project add --name "my-project"
.\Titanium.exe project use --project "my-project"
```

### Create a Document (From File Source)

```shell
.\Titaium.exe doc add --source "D:\Scans\File1.tiff"
#I8GDCPqqsUyjiEZWF6-dfg
```

### Add OCR Aspect to Document

The OCR Aspect only requires the `docId` parameter. All valid Tesseract image types will be processed from the
`{document}/_masters` directory.

```shell
.\Titanium.exe doc aspect ocr --docId "I8GDCPqqsUyjiEZWF6-dfg"
```