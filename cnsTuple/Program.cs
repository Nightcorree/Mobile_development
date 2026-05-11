int x = 1;
var y = 2;
// (1)
var x1 = (2, 3);
(int, int) x1b = (2, 3);
var х1с = (2, 3L, "Миша", 3.14);
Console.WriteLine(x1.Item1);
Console.WriteLine(x1.Item2);
Console.WriteLine();
// (2) название полей кортежа
(int min, int max) x2 = (2, 3);
Console.WriteLine(x2.min);
Console.WriteLine(x2.max);
Console.WriteLine();
Console.WriteLine("Hello, World!");
(int min, int max) x2 = (2, 3);
Console.WriteLine(x2.min);
Console.WriteLine(x2.max);
Console.WriteLine();
// (3) название полей кортежа через инициализацию
var x3 = (min: 2, max: 3);
Console.WriteLine(x3.min);
Console.WriteLine(x3.max);
Console.WriteLine();
// (4) распаковка кортежа
var (min, max) = (2, 3);
Console.WriteLine(min);
Console.WriteLine(max);
Console.WriteLine();
// (5) получение кортежа
var x5 = GetX5();
(int, int) GetX5() => (1, 2);
var x6 = GetX6();

// (7)
var x7 = GetX7(1, (2, 3));
(int, int) GetX5() => (1, 2);

(int min, int max) GetX6() => (1, 2);
(int min, int max) GetX7(int a, (int min, int max) р) => (a + p.min, a + p.max);