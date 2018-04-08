using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using uLearn.CSharp.Validators;

namespace uLearn.CSharp
{
    [TestFixture]
    public class VarInVariableDeclarationValidator_should
    {
        private VarInVariableDeclarationValidator validator;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            validator = new VarInVariableDeclarationValidator();
        }


        [TestCase("class A {void SomeMethod(){ for (Complex complex = new Complex(); complex.GetLength() < 10; complex *= a){}}}")]
        [TestCase(@"class A {void SomeMethod(){ List<string> c = new List<string>();}}")]
        public void Warn_declaration_with_type(string code)
        {
            var errors = validator.FindErrors(code);

            errors.Select(e => e.Span.StartLinePosition.Line).Should().Contain(0);
        }


        [TestCase(@"class A {void SomeMethod(){ int b;}}")]
        [TestCase(@"class A {public int Value = 1;}")]
        [TestCase(@"class A {void SomeMethod(){ int? i = 1; bool? flag = true;}}")]
        [TestCase(@"class A {void SomeMethod(){ var c = 0;}}")]
        [TestCase(@"class A {void SomeMethod(){ const int x = 5;}}")]
        [TestCase(@"class A {void SomeMethod(){ Exception a = new ArgumentNullException();}}")]
        [TestCase(@"class A {void SomeMethod(){ IList<string> c = new List<string>();}}")]
        [TestCase("class A {void SomeMethod(){ string a = \"some string\";}}")]
        [TestCase("class A {" +
                  "void SomeMethod(){string a = GetString();}" +
                  "private string GetString(){return \"abc\";}}")]
        public void ignore_correct_declaration(string code)
        {
            validator.FindErrors(code).Should().BeNullOrEmpty();
        }

        [TestCase("class A {void SomeMethod(){ int c = 0;}}")]
        [TestCase(@"class A {void SomeMethod(){ uint hash = 2166136261;}}")]
        [TestCase("class A {void SomeMethod(){ bool a = true;}}")]
        [TestCase(@"class A {void SomeMethod(){ double b = 0;}}")]
        public void ignore_primitives_declaration(string code)
        {
            validator.FindErrors(code).Should().BeNullOrEmpty();
        }
    }
}