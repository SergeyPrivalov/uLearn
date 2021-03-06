namespace uLearn.CSharp.ExcessLinesValidation.TestData.Correct
{
	public class BaseClass { }

	public class SomeClass
		: BaseClass
	{
	}

	public class SomeClass2
	{
	}

	public class SomeClass3
	{
		public void SomeMethod() { }

		public void SomeMethod1(int arg1,
								int arg2,
								int arg3)
		{
		}

		public void SomeMethod1<TData>(int arg1,
									   int arg2,
									   TData arg3)
			where TData : class
		{
		}

		public void SomeMethod2()
		{
		}

		public void SomeMethod3<TData>()
			where TData : class
		{
		}


		public int SomeMethod5()
		{
			SomeMethod();
			return 0;
		}

		public void SomeMethod6<TData1, TData2>()
			where TData1 : class
			where TData2 : class
		{
		}

		public void SomeMethod7()
		{
			for (;;)
			{
			}
			for (;;)
			{
			}
		}

		public void SomeMethod8() {
			SomeMethod7();
		}

		public void SomeMethod9()
		{
			//comment
			SomeMethod7();
			//comment
		}

		public void SomeMethod10()
		{
			/*comment
			comment*/
			SomeMethod7();
			/*comment
			comment*/
		}

		public void SomeMethod11()
		{
			if (true)
			{ SomeMethod7(); }
			if (true)
			{
				SomeMethod7(); }
			if (true)
			{ SomeMethod7();
			}
			if (true) {
				SomeMethod7();}
		}

		public void SomeMethod12()
		{
			{
				SomeMethod7();
			}
			{
				SomeMethod7();
			}
			{
			}
			{
			}
		}
	}

	public class SomeClass6
	{
	}

	public class SomeClass7
	{
	}

	public interface IInterface1
	{
	}

	public interface IInterface2
	{
	}

	public enum Enum1
	{
	}

	public enum Enum2
	{
	}

	public class SomeClass8 {
	}
}

namespace MyNamespace1
{
}

namespace MyNamespace1
{
}