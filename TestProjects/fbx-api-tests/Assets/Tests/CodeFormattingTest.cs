using System.Collections.Generic;
using NUnit.Framework;
using Unity.Coding.Editor.Formatting;

namespace ValidationTests
{
    class FormattingTests
    {
        [Test]
        public void Formatting_FormatsNothing()
        {
            var files = new List<string>();
            var fileNames = "";
            Formatting.ValidateAllFilesFormatted("Packages", files);

            if (files.Count > 0)
            {
                foreach (var file in files)
                {
                    fileNames += $"{file}\n";
                }
            }

            Assert.AreEqual(0, files.Count,
                "Some files have not been formatted correctly, please make sure to run the formatter on the " +
                "following files\n" + fileNames);
        }
    }
}
