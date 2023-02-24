using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Unity.Coding.Editor.ApiScraping;
using UnityEditor;

namespace ValidationTests
{
    class APIScrapingTests
    {
        static readonly string[] k_ApiFilesGUID =
        {
            "658145f18d591e14c9c3508c20f4a0c8", //Unity.Formats.Fbx.Editor.api
            "12de0f498bf429e48971636be1e9f6d2", // Unity.Formats.Fbx.Runtime.api
        };

        string[] m_ApiFileContents = new string[k_ApiFilesGUID.Length];

        [SetUp]
        public void Setup()
        {
            for (var i = 0; i < k_ApiFilesGUID.Length; i++)
            {
                var apiAssetPath = AssetDatabase.GUIDToAssetPath(k_ApiFilesGUID[i]);
                m_ApiFileContents[i] = File.ReadAllText(apiAssetPath);
            }
        }

        [TearDown]
        public void TearDown()
        {
            for (var i = 0; i < m_ApiFileContents.Length; i++)
            {
                var apiAssetPath = AssetDatabase.GUIDToAssetPath(k_ApiFilesGUID[i]);
                File.WriteAllText(apiAssetPath, m_ApiFileContents[i]);
            }
        }

        [Test]
        public void APIScraping_AllFilesUpToDate()
        {
            var files = new List<string>();
            var allScraped = ApiScraping.ValidateAllFilesScraped(files);
            var fileNames = "";
            var shouldBe = "";

            if (files.Count > 0)
            {
                foreach (var file in files)
                {
                    fileNames += $"{file}\n";
                    shouldBe += "<<<<<<<<<<<<<<<<\n" + File.ReadAllText(file) + "\n<<<<<<<<<<<<<<<<<<<<<<";
                }
            }

            Assert.IsTrue(allScraped,
                "Some .api files have not been generated. Please make sure to run ApiScraping.Scrape() or configure " +
                "the com.unity.coding package to your project in order to regenerate api files. Here are the api " +
                "files that need to be generated\n" + fileNames + "ShouldBe:" + shouldBe);
        }
    }
}
