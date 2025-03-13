# Instructions for Adding a New Template

Follow these steps to add a new template (using `HotlineTesting` as an example):

## 1\. Add the Template File

- Place the new template file into the `Templates` directory.
- Ensure you match the existing folder structure used by other templates.
- **Important:** The template file name **must end with `Template.cshtml`** to be correctly recognized by the Razor service.

## 2\. Register the Template

- Add the name of your new template to the `PdfTemplateRegistry.ValidTemplates` collection.

## 3\. Add the Model

- Create and add the model file into the `Models` directory.
- Examine existing models to maintain consistency in structure and naming conventions.
- **Important:** The model file name **must end with `Instance.cs`** to be correctly recognized by the Razor service.
- Add the instance model only from the `ITextDesigner` to the appropriate folder within the `Models` directory.

## Quick Reference

| Type      | Directory             | Naming Convention             |
|-----------|-----------------------|-------------------------------|
| Template  | `Templates`           | `NameOfTemplateTemplate.cshtml`|
| Model     | `Models`              | `NameOfModelInstance.cs`      |

**Example:**

- Template: `HotlineTestingTemplate.cshtml`
- Model: `HotlineTestingInstance.cs`
