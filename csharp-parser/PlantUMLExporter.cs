using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

class PlantUMLExporter
{
    static void Main(string[] args)
    {
        // Задаём путь к папке с .cs файлами и путь для сохранения .puml файла
        string folderPath = @"/home/alex/PROGRAM/LabsManager/Server/LabsManager"; // Укажи путь к папке с кодом
        string outputFilePath = @"/home/alex/plantuml/diagram.puml"; // Укажи путь для сохранения файла PlantUML

        string plantUmlDiagram = GeneratePlantUmlFromFolder(folderPath);
        
        // Записываем диаграмму в файл
        File.WriteAllText(outputFilePath, plantUmlDiagram);

        Console.WriteLine($"Диаграмма успешно сохранена в файл: {outputFilePath}");
    }

    static string GeneratePlantUmlFromFolder(string folderPath)
    {
        var plantUmlBuilder = new List<string>();
        plantUmlBuilder.Add("@startuml");

        // Рекурсивно ищем все .cs файлы в директории и поддиректориях
        var csFiles = Directory.GetFiles(folderPath, "*.cs", SearchOption.AllDirectories);
        var classes = new List<ClassDeclarationSyntax>();
        var rootNodes = new List<CompilationUnitSyntax>();

        foreach (var csFile in csFiles)
        {
            string code = File.ReadAllText(csFile);
            SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
            var root = (CompilationUnitSyntax)tree.GetRoot();
            rootNodes.Add(root);

            // Добавляем все классы из текущего файла
            classes.AddRange(root.DescendantNodes().OfType<ClassDeclarationSyntax>());
        }

        // Генерация классов и их элементов (методы, поля, свойства)
        foreach (var classDeclaration in classes)
        {
            plantUmlBuilder.Add($"class {classDeclaration.Identifier.Text} {{");

            foreach (var member in classDeclaration.Members)
            {
                if (member is PropertyDeclarationSyntax property)
                {
                    plantUmlBuilder.Add($"  {property.Identifier.Text} : {property.Type}");
                }
                else if (member is MethodDeclarationSyntax method)
                {
                    plantUmlBuilder.Add($"  {method.Identifier.Text}() : {method.ReturnType}");
                }
            }

            plantUmlBuilder.Add("}");
        }

        // Определение наследования и реализаций интерфейсов
        foreach (var classDeclaration in classes)
        {
            if (classDeclaration.BaseList != null)
            {
                foreach (var baseType in classDeclaration.BaseList.Types)
                {
                    plantUmlBuilder.Add($"{classDeclaration.Identifier.Text} --|> {baseType.Type}");
                }
            }
        }

        // Определение ассоциаций (классы, используемые в свойствах)
        foreach (var classDeclaration in classes)
        {
            foreach (var member in classDeclaration.Members.OfType<PropertyDeclarationSyntax>())
            {
                var propertyType = member.Type.ToString();
                if (IsClassType(propertyType, rootNodes))
                {
                    plantUmlBuilder.Add($"{classDeclaration.Identifier.Text} --> {propertyType}");
                }
            }
        }

        plantUmlBuilder.Add("@enduml");
        return string.Join(Environment.NewLine, plantUmlBuilder);
    }

    // Проверка, является ли типом другой класс из найденных в проекте
    static bool IsClassType(string typeName, List<CompilationUnitSyntax> roots)
    {
        return roots.SelectMany(root => root.DescendantNodes().OfType<ClassDeclarationSyntax>())
                    .Any(c => c.Identifier.Text == typeName);
    }
}
