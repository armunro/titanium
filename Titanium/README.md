## Introduction
Document scanning and processing tool.

## Quick Start

### At a Glance
1. All core dependancies (Tesseract data), resources (GPT Prompts), and project files are located in `TITANIUM_HOME` 
2. Directories that start with `_` are system directories.
3. `Document(s)` consist of many `Master(s)`. Masters are the original, unaltered files that make up the complete multi-page document.
4. `Aspect(s)` are layers of information that are added, by processing the original master files.
5. `Aspect(s)` may refer/depend on the output of other aspects.

### Install Core Dependancies
```shell
.\Titanium.exe install --lang eng
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
The OCR Aspect only requires the `docId` parameter. All valid Tesseract image types will be processed from the `{document}/_masters` directory.
```shell
.\Titanium.exe doc aspect ocr --docId "I8GDCPqqsUyjiEZWF6-dfg"
```
