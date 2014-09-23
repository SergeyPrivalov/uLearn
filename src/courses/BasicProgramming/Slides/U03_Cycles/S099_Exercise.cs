﻿namespace uLearn.Courses.BasicProgramming.Slides.U03_Cycles
{
	[Slide("Практика", "{D79D448F-B8B6-4D8B-BDD4-84BB35528C5A}")]
	class S099_Exercise
	{
		/*
		[Скачать проекты с задачами](loops.zip)

		### Делители (1 балл)

		Для введенного с консоли числа:

		* Выведите все его делители по одному разу. 
		* Для каждого делителя, найдите максимальную степень, в которую можно возвести делитель, чтобы он остался делителем.

		Решите задачу в отдельном консольном проекте.

		### Лабиринты (2 балла)

		1. Откройте и изучите проект Mazes. Там заготовлены несколько фиксированных лабиринтов.
		2. В каждом лабиринте вам нужно довести робота до выхода — клетки, помеченной зеленым кружком.
		3. Для этого реализуйте пустые методы в классе MazeTasks. Используйте методы объекта robot, для его перемещения.

		Подсказка:

		* Используйте циклы.
		* Постарайтесь решить задачу с ограничением "не более одного цикла на метод".
		* Подумайте, может быть стоит создать какие-то вспомогательные методы.


		### Dragon curve (1 балл)

		В этой задаче вам нужно будет нарисовать вот такую фигуру:

		![Dragon curve](dragon.png)

		Вряд ли это пригодится вам в будущем, но зато красиво! :)

		Подробнее про этот замечательный фрактал вы можете почитать, например, [в википедии](http://en.wikipedia.org/wiki/Dragon_curve). 

		А один из алгоритмов построения этой сложной фигуры крайне прост:
		
			Начните с точки (1, 0)
			На каждой итерации:
		
			1. Выберите случайно одно из следующих преобразований и примените его к текущей точке:
			
				Преобразование 1. (поворот на 45° и сжатие в sqrt(2) раз):
				x' = (x · cos(45°) - y · sin(45°)) / sqrt(2)
				y' = (x · sin(45°) + y · cos(45°)) / sqrt(2)
			
				Преобразование 2. (поворот на 135°, сжатие в sqrt(2) раз, сдвиг по X на единицу):
				x' = (x · cos(135°) - y · sin(135°)) / sqrt(2) + 1
				y' = (x · sin(135°) + y · cos(135°)) / sqrt(2)
			
			2. Нарисуйте текущую точку.

		В проекте DragonFractal уже есть часть кода для работы с изображением.

		Вперед!

	
		Кстати, похожим образом можно построить еще множество фракталов. Но самым известным, наверное, является [фрактальный папоротник](http://algolist.manual.ru/graphics/fern.php):

		![Fern](fern.png)

		*/
	}
}