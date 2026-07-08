
using System;
using System.IO;
using System.Xml;
using System.Text;

namespace SplitTcxFile;

public sealed class Program
{
    public static void Main(String[] args)
    {
        /* Get the full pathname of the file to process, and check it exists. */
        var folder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        var path = $"failed{Path.DirectorySeparatorChar}Road 180416-180926.tcx";

        var fullPathName = Path.Combine(folder, path);

        if (File.Exists(fullPathName) is false)
        {
            Console.WriteLine($"File {fullPathName} doesn't exist.");
            return;
        }


        /* First pass through the file.  Check that the file is a valid TCX file, and get the number of Activity elements in it. */
        var activityElementsCount = 0;
        try
        {
            using var xmlReader = XmlReader.Create(fullPathName);

            var inActivitiesElement = false;
            var gotRootElement = false;

            for (; ; )
            {
                if (xmlReader.Read() is false)
                    break;

                if (xmlReader.NodeType == XmlNodeType.Element)
                {
                    if (gotRootElement is false)
                    {
                        gotRootElement = true;
                        if (xmlReader.Name != "TrainingCenterDatabase")
                        {
                            Console.WriteLine("Not a TCX file; missing TrainingCenterDatabase root element.");
                            return;
                        }
                        continue;
                    }

                    if (xmlReader.Name == "Activities")
                    {
                        inActivitiesElement = true;
                        continue;
                    }

                    if (xmlReader.Name == "Activity" && inActivitiesElement)
                        ++activityElementsCount;

                    continue;
                }

                if (xmlReader.NodeType == XmlNodeType.EndElement)
                {
                    if (xmlReader.Name == "Activities")
                        inActivitiesElement = false;

                    continue;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            return;
        }


        /* Nothing to do if no Activity elements were found. */
        if (activityElementsCount == 0)
        {
            Console.WriteLine("No 'Activity' elements were found, so no new files will be generated.");
            return;
        }


        /* Subsequent passes through the input file, each pass generating a new output file that contains just one activity. */
        for (var activityIndex = 0; activityIndex < activityElementsCount; ++activityIndex)
        {
            try
            {
                CopyInputFileToOutputFileWithOneActivity(fullPathName, activityIndex);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return;
            }
        }
    }

    // 
    private static void CopyInputFileToOutputFileWithOneActivity(String inputFilePathName, Int32 activityIndex)
    {
        /* Get the full pathname of the output file, and create the file. */
        var sep = Path.DirectorySeparatorChar;
        var outputFolderName = Path.GetDirectoryName(inputFilePathName);
        var outputSubFolderName = Path.GetFileNameWithoutExtension(inputFilePathName);
        var outputFilePathName = $"{outputFolderName}{sep}{outputSubFolderName}{sep}{outputSubFolderName}_activity_{(activityIndex + 1)}.tcx";

        Directory.CreateDirectory(Path.GetDirectoryName(outputFilePathName));
        var fs = File.Create(outputFilePathName);
        fs.Close();
        fs.Dispose();


        /* Copy all nodes to the output file except for Activity elements that aren't referenced by activityIndex. */
        using var xmlReader = XmlReader.Create(inputFilePathName);

        var writerSettings = new XmlWriterSettings
        {
            Indent = true,
            Encoding = new UTF8Encoding(false),     // omit BOM
            NewLineChars = "\n"                     // UNIX (LF)
        };
        using var xmlWriter = XmlWriter.Create(outputFilePathName, writerSettings);

        var activityElementsCount = 0;
        var inActivitiesElement = false;
        for (; ; )
        {
            if (xmlReader.Read() is false)
                break;

            if (xmlReader.NodeType == XmlNodeType.Element)
            {
                if (xmlReader.Name == "Activities")
                    inActivitiesElement = true;

                if (xmlReader.Name == "Activity" && inActivitiesElement)
                {
                    ++activityElementsCount;

                    if ((activityElementsCount - 1) != activityIndex)
                    {
                        xmlReader.Skip();
                        continue;
                    }
                }
            }

            if (xmlReader.NodeType == XmlNodeType.EndElement)
            {
                if (xmlReader.Name == "Activities")
                    inActivitiesElement = false;
            }

            writeNode(xmlReader, xmlWriter);
        }

        xmlWriter.Flush();
        xmlWriter.Close();


        // local fn
        static void writeNode(XmlReader reader, XmlWriter writer)
        {
            switch (reader.NodeType)
            {
                case XmlNodeType.None:
                    break;

                case XmlNodeType.Element:
                    writer.WriteStartElement(reader.Prefix, reader.LocalName, reader.NamespaceURI);
                    writer.WriteAttributes(reader, defattr: true);
                    if (reader.IsEmptyElement)
                    {
                        writer.WriteEndElement();
                    }
                    break;

                case XmlNodeType.Text:
                    writer.WriteString(reader.Value);
                    break;

                case XmlNodeType.CDATA:
                    writer.WriteCData(reader.Value);
                    break;

                case XmlNodeType.EntityReference:
                    writer.WriteEntityRef(reader.Name);
                    break;

                case XmlNodeType.Entity:
                    break;

                case XmlNodeType.ProcessingInstruction:
                    writer.WriteProcessingInstruction(reader.Name, reader.Value);
                    break;

                case XmlNodeType.Comment:
                    writer.WriteComment(reader.Value);
                    break;

                case XmlNodeType.Document:
                    break;

                case XmlNodeType.DocumentType:
                    writer.WriteDocType(reader.Name, reader.GetAttribute("PUBLIC"), reader.GetAttribute("SYSTEM"), reader.Value);
                    break;

                case XmlNodeType.DocumentFragment:
                    break;

                case XmlNodeType.Notation:
                    break;

                case XmlNodeType.Whitespace:
                    break;

                case XmlNodeType.SignificantWhitespace:
                    writer.WriteWhitespace(reader.Value);
                    break;

                case XmlNodeType.EndElement:
                    writer.WriteFullEndElement();
                    break;

                case XmlNodeType.EndEntity:
                    break;

                case XmlNodeType.XmlDeclaration:
                    writer.WriteProcessingInstruction(reader.Name, reader.Value);
                    break;
            }
        }
    }
}
