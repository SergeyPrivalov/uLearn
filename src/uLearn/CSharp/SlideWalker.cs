using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace uLearn.CSharp
{
	public class SlideWalker : CSharpSyntaxWalker
	{
		public readonly List<SlideBlock> Blocks = new List<SlideBlock>();
		public SlideBlock Exercise { get; private set; }
		public SlideBlock TestBlock { get; private set; }
		public readonly List<string> Hints = new List<string>();
		public MethodDeclarationSyntax ExerciseNode;
		public MethodDeclarationSyntax TestNode;

		public readonly List<MemberDeclarationSyntax> samples = new List<MemberDeclarationSyntax>();

		public SlideWalker() : base(SyntaxWalkerDepth.Trivia)
		{
		}

		public override void VisitClassDeclaration(ClassDeclarationSyntax node)
		{
			base.VisitClassDeclaration(node);
			if (node.HasAttribute<SampleAttribute>())
			{
				samples.Add(node);
				Blocks.Add(CreateSampleBlock(node));
			}
		}

		public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
		{
			base.VisitMethodDeclaration(node);
			if (node.HasAttribute<SampleAttribute>())
			{
				samples.Add(node);
				Blocks.Add(CreateSampleBlock(node));
			}
			else if (node.HasAttribute<ExerciseAttribute>())
			{
				ExerciseNode = node;
				Exercise = SlideBlock.FromCode(GetExerciseCode(node));
				var hints = node.GetAttributes<HintAttribute>().Select(a => a.GetHint());
				Hints.AddRange(hints);
			}
			else if (node.HasAttribute<TestAttribute>())
			{
				TestNode = node;
				var testCode = node.Body.ToFullString().RemoveCommonNesting() + "\n";
				TestBlock = SlideBlock.FromCode(testCode);
			}
		}

		private SlideBlock CreateSampleBlock(MethodDeclarationSyntax node)
		{
			string code = node.Body.Statements.ToFullString().RemoveCommonNesting();
			return SlideBlock.FromCode(code);
		}

		private SlideBlock CreateSampleBlock(ClassDeclarationSyntax node)
		{
			string code = node.WithAttributeLists(new SyntaxList<AttributeListSyntax>()).ToFullString().RemoveCommonNesting();
			return SlideBlock.FromCode(code);
		}

		private string GetExerciseCode(MethodDeclarationSyntax method)
		{
			var codeLines = method.TransformExercise().ToFullString().SplitToLines().RemoveCommonNesting();

			return string.Join("\n", FilterSpecialComments(codeLines));
		}

		private IEnumerable<string> FilterSpecialComments(IEnumerable<string> lines)
		{
			var inUncomment = false;
			foreach (var line in lines)
			{
				if (!inUncomment && line.Trim() == "/*uncomment")
				{
					inUncomment = true;
				}
				else if (inUncomment && line.Trim() == "*/")
				{
					inUncomment = false;
				}
				else
					yield return line;
			}
		}


		public override void VisitTrivia(SyntaxTrivia trivia)
		{
			base.VisitTrivia(trivia);
			if (trivia.CSharpKind() == SyntaxKind.MultiLineCommentTrivia)
			{
				var firstLine = trivia.ToString().SplitToLines().First().Trim();
				if (firstLine != "/*uncomment")
					Blocks.Add(ExtractMarkDownFromComment(trivia));
			}
		}

		public static SlideBlock ExtractMarkDownFromComment(SyntaxTrivia comment)
		{
			int identation = comment.SyntaxTree.GetLineSpan(comment.FullSpan).StartLinePosition.Character;
			string[] commentLines = comment.ToString().SplitToLines();
			var sb = new StringBuilder();
			foreach (string line in commentLines.Skip(1).Take(commentLines.Length - 2))
			{
				if (line.Trim() != "")
				{
					if (line.Length < identation || line.Substring(0, identation).Trim() != "")
						throw new Exception("Wrong identation in line: " + line);
					sb.AppendLine(line.Substring(identation));
				}
				else
				{
					sb.AppendLine();
				}
			}
			return SlideBlock.FromMarkdown(sb.ToString());
		}
	}
}