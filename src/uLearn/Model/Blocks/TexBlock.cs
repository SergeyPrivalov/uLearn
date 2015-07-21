using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using uLearn.Model.Edx.EdxComponents;

namespace uLearn.Model.Blocks
{
	[XmlType("tex")]
	public class TexBlock : SlideBlock
	{
		[XmlElement("line")]
		public string[] TexLines { get; set; }

		public TexBlock(string[] texLines)
		{
			TexLines = texLines;
		}

		public TexBlock()
		{
		}

		public override string ToString()
		{
			return string.Format("Tex {0}", string.Join("\n", TexLines));
		}

		public override IEnumerable<Component> ToEdxComponent(string folderName, string courseId, string displayName, Slide slide, int componentIndex)
		{
			var urlName = slide.Guid + componentIndex;
			return new [] { new HtmlComponent(folderName, urlName, displayName, urlName, string.Join("\n", TexLines.Select(x => "$$" + x + "$$")).GetHtmlWithUrls("/static").Item1) };
		}
	}
}