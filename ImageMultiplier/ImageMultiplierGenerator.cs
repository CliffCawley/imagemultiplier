﻿using System;
using System.Linq;
using System.CodeDom.Compiler;
using MonoDevelop.Core;
using MonoDevelop.Ide.CustomTools;
using MonoDevelop.Projects;
using System.Text;
using Svg;
using System.Drawing.Imaging;
using System.IO;
using Svg.Transforms;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ImageMultiplier
{
	public class ImageMultiplierGenerator : ISingleFileCustomTool
	{
		public Task Generate(ProgressMonitor monitor, ProjectFile file, SingleFileCustomToolResult result)
		{
			return Task.Run(() =>
			{

				// This is the project directory containing the .imfl file
				string dir = System.IO.Path.GetDirectoryName(file.FilePath);
				monitor.Log.WriteLine("Creating images for " + file.FilePath);

				var processor = new ImageProcessor(monitor, result);

				var lines = System.IO.File.ReadLines(file.FilePath);
				int lineNumber = 0;
				var outputSpecifiers = new List<OutputSpecifier>();
				foreach (var line in lines)
				{
					lineNumber++;
					if (string.IsNullOrWhiteSpace(line))
						continue;
					if (line.StartsWith("#", StringComparison.Ordinal))
						continue;

					//monitor.Log.WriteLine("Interpreting " + line);

					try
					{
						// Slight hack ...
						if (line.Contains("type:"))
						{
							var ts = JsonConvert.DeserializeObject<OutputSpecifier>(line);
							if (ts != null)
							{
								string testPath = processor.GetFullOutputPath(dir, ts, "test.svg");
								var directory = Path.GetDirectoryName(testPath);
								outputSpecifiers.Add(ts);
								monitor.Log.WriteLine("Added output specifier " + line);
							}
							else {
								result.Errors.Add(new CompilerError(file.FilePath, lineNumber, 1, "Err2", "Could not parse output specifier"));
							}
						}
						else if (line.Contains("process:"))
						{
							var ps = JsonConvert.DeserializeObject<ProcessSpecifier>(line);
							if (ps != null)
							{
								// Process output
								var subdir = Path.GetDirectoryName(ps.process);
								var searchPattern = Path.GetFileName(ps.process);
								var inputDirectory = Path.Combine(dir, subdir);

								if (Directory.Exists(inputDirectory))
								{
									var outputters = outputSpecifiers.Where(s => ps.@as.Contains(s.type));
									processor.Process(dir, Directory.GetFiles(inputDirectory, searchPattern), outputters, lineNumber);
								}
								else {
									result.Errors.Add(new CompilerError(file.FilePath, lineNumber, 1, "Err17", "Directory not found " + subdir));
								}

							}
							else {
								result.Errors.Add(new CompilerError(file.FilePath, lineNumber, 1, "Err2", "Could not parse process specifier"));
							}
						}
						else {
							result.Errors.Add(new CompilerError(file.FilePath, lineNumber, 1, "Err2", "Could not parse this line"));
						}
					}
					catch (Exception ex)
					{
						result.Errors.Add(new CompilerError(file.FilePath, lineNumber, 1, "Err1", ex.ToString()));
					}
				}

				result.GeneratedFilePath = "";

			});
		}

 
	}
}