using ExactImporterLib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ExactImporterTest
{
    [TestClass]
    public class UnitTest1
    {
		[TestMethod]
        public void TestImport()
        {
			string aName = "a.jpg";
			string bName = "b.mov";
			byte[] a = Guid.NewGuid().ToString().Select(x => (byte)x).ToArray();
			byte[] b = Guid.NewGuid().ToString().Select(x => (byte)x).ToArray();

			string targetPath = "target";
			string src1 = Guid.NewGuid().ToString("N");
			string src2 = Guid.NewGuid().ToString("N");

			Directory.CreateDirectory(src1);
			Directory.CreateDirectory(src2);
			Directory.CreateDirectory(targetPath);

			File.WriteAllBytes(Path.Combine(src1, aName), a);
			File.WriteAllBytes(Path.Combine(src2, bName), b);

			ImporterConfig config = new ImporterConfig
			{
				ImportExtensionsWithDot = new HashSet<string> { ".jpg", ".mov" },
				SourcePaths = new List<string> { src1, src2 },
				TargetPath = targetPath,
				NonExisting = NonExisting.Copy
			};
			Importer importer = new Importer(config);
			importer.Import();
		}
    }
}
