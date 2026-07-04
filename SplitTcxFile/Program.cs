
using System;
using System.IO;
using System.Xml;

namespace SplitTcxFile;

public sealed class Program
{
    public static void Main(String[] args)
    {
        /* Get the full pathname of the file to process, and check it exists. */

        var folder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        var path = $"failed{Path.DirectorySeparatorChar}Road 150221-160130.tcx";

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


        //
        Console.WriteLine($"Number of activities: {activityElementsCount}.");


        /* Subsequent passes through the input file, each pass generating a new output file. */

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
        // open input file
        // read in nodes
        // copy all nodes to output file (in same folder; name based on activityIndex), except for Activity elements that
        // are referenced by activityIndex
        Console.WriteLine($"{activityIndex}");
    }
}
