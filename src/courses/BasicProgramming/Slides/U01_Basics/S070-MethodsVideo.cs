﻿using System;

namespace uLearn.Courses.BasicProgramming.Slides
{
	[Slide("Методы", "{3a14a28f-c11d-45b3-b3a9-1a3fac676ee7}")]
	public class S07_MethodsVideo
	{
		//#video //www.youtube.com/embed/2Rh3otEbeaU?rel=0

		/*
		## Заметки по лекции
		*/

		class Program
		{

			// Это метод, возвращающий значение типа int, принимающий два аргумента типа double.
			// Его можно называть функцией, но это название не очень распространено.
			// Сигнатура метода - это совокупность последовательности типов аргументов и типа возвращаемого значения 
			static int DivideAndRound(double a, double b)
			{
				// return указывает, какое значение будет возвращено
				return (int)Math.Round(a / b);
			}

			// Это метод, не возвращающий значения. Вместо типа возвращаемого значения стоит void
			static void WriteNumber(int number)
			{
				Console.WriteLine(number);

				// return указывается без значения, и его следует опускать, когда это возможно
				return;
			}


			static void WriteNumber(int number, int anotherNumber)
			{
				Console.WriteLine(number);
				Console.WriteLine(anotherNumber);
			}

			static void Main()
			{
				WriteNumber(DivideAndRound(10.5, 2.1));
			}
		}
	}
}
