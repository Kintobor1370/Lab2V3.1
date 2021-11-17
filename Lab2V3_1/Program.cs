using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using static System.Math;
using System.Linq;
using System.IO;

//________ЭЛЕМЕНТ ДАННЫХ ИЗМЕРЕНИЯ (СОДЕРЖИТ КОРДНИАТЫ ПО ОСИ X И Y И ЗНАЧЕНИЕ ВЕКТОРА ПОЛЯ В ЗАДАННОЙ ТОЧКЕ)________
public struct DataItem
{
    public double X { get; }
    public double Y { get; }
    public Vector2 VecVal { get; }

    public DataItem(double x, double y, Vector2 v)
    { X = x; Y = y; VecVal = v; }

    public string ToLongString(string format)
    {
        double VecAbs = Sqrt(Pow(VecVal.X, 2) + Pow(VecVal.Y, 2));
        return "X=" + String.Format(format, X) +"   Y=" + String.Format(format, Y) + "   Vector Value: " + 
               String.Format(format, VecVal) + "   Vector Module: " + String.Format(format, VecAbs) +"\n";
    }

    public override string ToString() { return "\n"; }
}

//_____________________________________ДЕЛЕГАТ ВЕКТОРА ПОЛЯ_____________________________________
public delegate Vector2 FdblVector2(double x, double y);

//________________________ЭЛЕМЕНТ ДАННЫХ (СОДЕРЖИТ НАЗВАНИЕ ТИПА И ДАТУ)________________________
public abstract class V3Data : IEnumerable<DataItem>
{
    public string ObjectId { get; protected set; }
    public DateTime Date { get; protected set; }

    public V3Data(string ObjectId, DateTime Date)
    { this.ObjectId = ObjectId; this.Date = Date; }

    public abstract int Count { get; }

    public abstract double MaxDistance { get; }

    public abstract string ToLongString(string format);

    public override string ToString()
    { return "ID: " + ObjectId + "\nDate: " + Convert.ToString(Date) + "\n"; }

    public abstract IEnumerator<DataItem> GetEnumerator();
    
    IEnumerator IEnumerable.GetEnumerator()
    { return this.GetEnumerator(); }
}

//__________________________________________СПИСОК__________________________________________
public class V3DataList: V3Data
{
    public List<DataItem> DataList { get; }

    public V3DataList(string str, DateTime date): base(str, date)
    { DataList = new List<DataItem>(); }

    public bool Add(DataItem NewItem)
    {
        bool val = true;
        foreach(DataItem Item in DataList)
        {
            if(Item.X == NewItem.X && Item.Y == NewItem.Y)
                val = false;
        }
        if (val)
            DataList.Add(NewItem);
        return val;
    }

    public int AddDefaults(int nItems, FdblVector2 F)
    {
        int num = 0;
        Random rnd = new Random();
        int x, y;
        Vector2 result;
        bool Item_is_added;

        for(int i=0; i<nItems; i++)
        {
            x = rnd.Next(-50, 50);
            y = rnd.Next(-50, 50);
            result = F(x, y);
            DataItem Item = new DataItem(x, y, result);
            Item_is_added = Add(Item);
            if(Item_is_added)
                num++;
        }
        return num;
    }

    public override int Count
    { get { return DataList.Count; } }

    public override double MaxDistance
    {
        get
        {
            double Dis, MaxDis = 0;
            foreach(DataItem A in DataList)
                foreach(DataItem B in DataList)
                {
                    Dis = Sqrt(Pow((A.X-B.X), 2) + Pow((A.Y-B.Y), 2));
                    if(Dis > MaxDis)
                        MaxDis = Dis;
                }
            return MaxDis;
        }
    }

    public override string ToString()
    { return base.ToString() + "\nAmount of elements in the list: " + Count + "\n"; }

    public override string ToLongString(string format)
    {
        string info = null;
        int n = 1;
        Vector2 abs;
        foreach(DataItem Item in DataList)
        {
            info += Convert.ToString(n) + ")  " + Item.ToLongString(format);
            n++;
        }
        return ToString() + "List Info:\n" + info;
    }

    public override IEnumerator<DataItem> GetEnumerator()
    { return DataList.GetEnumerator(); }

    public static bool SaveAsText(string filename, V3DataList v3)
    {
        FileStream fs = null;
        try
        {
            fs = new FileStream(filename, FileMode.Open);
            StreamWriter sw = new StreamWriter(fs);
            
            sw.WriteLine(v3.ObjectId);
            sw.WriteLine(v3.Date);
            sw.WriteLine(v3.Count);
            foreach (var item in v3.DataList)
            {
                sw.WriteLine(item.X);
                sw.WriteLine(item.Y);
                sw.WriteLine(item.VecVal.X);
                sw.WriteLine(item.VecVal.Y);
            }
            sw.Close();
        }
        catch(FileNotFoundException)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\nEXCEPTION: FAILED SAVE ATTEMPT. FILE \"{filename}\" DOESN'T EXIST IN THE CURRENT DIRECTORY!\n");
            Console.ResetColor();

            return false;
        }
        finally
        {
            if(fs != null)
               fs.Close();
        }
        return true;
    }

    public static bool LoadAsText(string filename, ref V3DataList v3)
    {
        FileStream fs = null;
        try
        {
            fs = new FileStream(filename, FileMode.Open);
            StreamReader sr = new StreamReader(fs);
            
            v3.ObjectId = sr.ReadLine();
            v3.Date = DateTime.Parse(sr.ReadLine());
            int count = int.Parse(sr.ReadLine());
            
            for (int i=0; i<count; i++)
            {
                DataItem NewItem = new DataItem(double.Parse(sr.ReadLine()), double.Parse(sr.ReadLine()), new Vector2(float.Parse(sr.ReadLine()), float.Parse(sr.ReadLine())));
                v3.Add(NewItem);
            }
            sr.Close();
        }
        catch(FileNotFoundException)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\nEXCEPTION: FAILED LOAD ATTEMPT. FILE \"{filename}\" DOESN'T EXIST IN THE CURRENT DIRECTORY!\n");
            Console.ResetColor();

            return false;
        }
        finally
        {
            if(fs != null)
               fs.Close();
        }
        return true;
    }
}

//__________________________________________ПРЯМОУГОЛЬНАЯ СЕТКА__________________________________________
public class V3DataArray: V3Data
{
    public int Xnum { get; protected set; }
    public int Ynum { get; protected set; }
    public double Xstep { get; protected set; }
    public double Ystep { get; protected set; }
    public Vector2[,] InfoVec { get; protected set; }
    
    public V3DataArray(string str, DateTime date): base(str, date)
    { Xnum = 0; Ynum = 0; Xstep = 0; Ystep = 0; InfoVec = new Vector2[0,0]; }

    public V3DataArray(string str, DateTime date, int xnum, int ynum, double xstep, double ystep, FdblVector2 Vec): base(str, date)
    {
        Xnum = xnum;
        Ynum = ynum;
        Xstep = xstep;
        Ystep = ystep;
        InfoVec = new Vector2[Xnum, Ynum];
        
        double Xval = 0;
        double Yval = 0;
        for(int i=0; i<Xnum; i++)
            for(int j=0; j<Ynum; j++)
            {
                InfoVec[i,j] = Vec(Xval, Yval);
                Xval += Xstep;
                Yval += Ystep;
            }
    }

    public override int Count { get { return InfoVec.Length; } }

    public override double MaxDistance
    { 
        get
        {
            double x1, y1, x2, y2, Dis, MaxDis = 0;
            for(int i1=0; i1<Xnum; i1++)
                for(int j1=0; j1<Ynum; j1++)
                {
                    x1 = i1*Xstep;
                    y1 = j1*Ystep;
                    for(int i2=0; i2<Xnum; i2++)
                        for(int j2=0; j2<Ynum; j2++)
                        {
                            x2 = i2*Xstep;
                            y2 = j2*Ystep;    
                            
                            Dis = Sqrt(Pow((x2-x1), 2) + Pow((y2-y1), 2));
                            if(Dis > MaxDis)
                                MaxDis = Dis;
                        }
                }
            return MaxDis;
        }
    }

    public override string ToString()
    { return base.ToString() + "\nAmount of nodes:\nOx: " + Convert.ToString(Xnum) + "     Oy: " + Convert.ToString(Ynum) + "\n"; }

    public override string ToLongString(string format)
    {
        string str = this.ToString() + "\nNodes info:\n";
        Vector2 abs;
        int n = 1;
        for(int i=0; i<Xnum; i++)
            for(int j=0; j<Ynum; j++)
            {
                double VecAbs = Sqrt(Pow(InfoVec[i,j].X, 2) + Pow(InfoVec[i,j].Y, 2));
                str += Convert.ToString(n) + ".  X=" + String.Format(format, i*Xstep) + 
                "   Y=" + String.Format(format, j*Ystep) + "  Vector Value: " + String.Format(format, InfoVec[i, j]) + 
                "   Vector Module: " + String.Format(format, VecAbs) + "\n";
                n++;
            }
        return str;
    }

    public static implicit operator V3DataList(V3DataArray DataArray)
    {
        V3DataList DataList = new V3DataList(DataArray.ObjectId, DataArray.Date);
        for(int i=0; i<DataArray.Xnum; i++)
            for(int j=0; j<DataArray.Ynum; j++)
            {
                DataItem Item = new DataItem(i*DataArray.Xstep, j*DataArray.Ystep, DataArray.InfoVec[i,j]);
                DataList.Add(Item);
            }
        return DataList;
    }

    public override IEnumerator<DataItem> GetEnumerator()
    {
        V3DataList Lst = this;
        return Lst.GetEnumerator();
    }

    public static bool SaveBinary(string filename, V3DataArray v3)
    {
        FileStream fs = null;
        try
        { 
            fs = new FileStream(filename, FileMode.Open);
            BinaryWriter save = new BinaryWriter(fs);
            {
                save.Write(v3.ObjectId);
                save.Write(v3.Date.ToBinary());
                save.Write(v3.Xnum);
                save.Write(v3.Ynum);
                save.Write(v3.Xstep);
                save.Write(v3.Ystep);
                
                foreach (var vec in v3.InfoVec)
                {
                    save.Write(vec.X);
                    save.Write(vec.Y);
                }
                save.Close();
            }
        }
        catch(FileNotFoundException)
        { 
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\nEXCEPTION: FAILED SAVE ATTEMPT. FILE \"{filename}\" DOESN'T EXIST IN THE CURRENT DIRECTORY!\n");
            Console.ResetColor();
            
            return false;
        }
        finally
        {
            if(fs != null)
                fs.Close();
        }
        return true;
    }
    public static bool LoadBinary(string filename, ref V3DataArray v3)
    {
        FileStream fs = null;
        try
        {
            fs = new FileStream(filename, FileMode.Open);
            BinaryReader load = new BinaryReader(fs);
            {
                v3.ObjectId = load.ReadString();
                v3.Date = DateTime.FromBinary(load.ReadInt64());
                v3.Xnum = load.ReadInt32();
                v3.Ynum = load.ReadInt32();
                v3.Xstep = load.ReadDouble();
                v3.Ystep = load.ReadDouble();
                
                v3.InfoVec = new Vector2[v3.Xnum, v3.Ynum];
                for(int i=0; i<v3.Xnum; i++)
                    for(int j=0; j<v3.Ynum; j++)
                        v3.InfoVec[i,j] = new Vector2(load.ReadSingle(), load.ReadSingle());
                
                load.Close();
            }
        }
        catch(FileNotFoundException)
        { 
             Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\nEXCEPTION: FAILED LOAD ATTEMPT. FILE \"{filename}\" DOESN'T EXIST IN THE CURRENT DIRECTORY!\n");
            Console.ResetColor();
            
            return false;
        }
        finally
        {
            if(fs != null)
                fs.Close();
        }
        return true;
    }
}

//______________________________________КОЛЛЕКЦИЯ ЭЛЕМЕНТОВ ДАННЫХ______________________________________
public class V3MainCollection
{
    private List<V3Data> DataList;

    public V3MainCollection()
    { DataList = new List<V3Data>(); }

    public V3Data this[int index] { get { return DataList[index]; } }

    public int Count { get { return DataList.Count; } }

    public bool Contains(string ID)
    {
        bool contains = false;
        foreach(V3Data Data in DataList)
            if(Data.ObjectId == ID)
            { contains = true; return contains; }
        return contains;
    }

    public bool Add(V3Data NewItem)
    {
        bool contains = false;
        bool Item_is_added = false;
        
        foreach(V3Data Data in DataList)
            if(Data.ObjectId == NewItem.ObjectId)
                contains = true;
        
        if (contains == false)
        { DataList.Add(NewItem); Item_is_added = true; }
        return Item_is_added;
    }

    public string ToLongString(string format)
    {
        string str = "\n____________________________________COLLECTION ITEMS:___________________________________\n";
        foreach(V3Data Item in DataList)
            str += Item.ToLongString(format) + "\n";
        return str + "___________________________________END OF COLLECTION.___________________________________\n";
    }

    public override string ToString()
    {
        string str = null;
        foreach(V3Data Item in DataList)
            str += Item.ToString();
        return str;
    }

    public double Avrg
    {
        get
        {
//          Console.WriteLine(this.DataList.Count);
            if(this.DataList.Count == 0)
                return double.NaN;
            
            var DataItems = from i in DataList from j in i select j;
            var Distances = DataItems.Select(i => Sqrt(Pow(i.X, 2) + Pow(i.Y, 2)));
            
            return Distances.Average();
        }
    }

    public IEnumerable<float> Absolute
    {
        get
        {
            if(this.DataList.Count == 0)
                return null;
            
            var NonEmptyDataList = from i in DataList where i.Count > 0 select i;
            var Modules = from i in NonEmptyDataList select (float)(i.Select(j => Sqrt(Pow(j.VecVal.X, 2) + Pow(j.VecVal.Y, 2))).Max() - i.Select(j => Sqrt(Pow(j.VecVal.X, 2) + Pow(j.VecVal.Y, 2))).Min());

            return Modules;
      }
    }

    public IEnumerable<IGrouping<double, DataItem>> Grouping
    {
        get
        {
            if(this.DataList.Count == 0)
                return null;

            var DataItems = from i in DataList from j in i select j;
            var XGroups = DataItems.GroupBy(i => i.X);

            return XGroups;
        }
    }
}

//_________________________________КЛАСС МЕТОДОВ ВЫЧИСЛЕНИЯ ВЕКТОРА ПОЛЯ_________________________________
public static class VecCalculator
{
    public static Vector2 var1(double x, double y)
    { return new Vector2((float)(x+y), (float)(x-y)); }

    public static Vector2 var2(double x, double y)
    { return new Vector2((float)(x*y), (float)(x+y)); }

    public static Vector2 var3(double x, double y)
    { return new Vector2((float)(x*y), (float)(x-y)); }
}

class Test
{
    static void SaveLoadObjects(string OutputFormat)
    {
//.....ПРОВЕРКА ЧТЕНИЯ/ЗАПИСИ СПИСКА         
        V3DataArray Temp = new V3DataArray("List save/load Test", DateTime.Now, 4, 3, 5, 2, new FdblVector2(VecCalculator.var1));
        V3DataList SaveLst = Temp;
        string filename = "textfile.txt";

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n_________________________________________LIST:________________________________________");
        Console.ResetColor();
        Console.WriteLine(SaveLst.ToLongString(OutputFormat));
        
        if(V3DataList.SaveAsText(filename, SaveLst))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("________________________________LIST SAVED SUCCESSFULLY!_______________________________\n" +
            $"                                (check {filename})\n\n");
            Console.ResetColor();
        }

        V3DataList LoadLst = new V3DataList(null, DateTime.Now);

        if(V3DataList.LoadAsText(filename, ref LoadLst))
        {
            Console.WriteLine(LoadLst.ToLongString(OutputFormat));
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("_______________________________LIST LOADED SUCCESSFULLY!_______________________________\n");
            Console.ResetColor();
        }


//.....ПРОВЕРКА ЧТЕНИЯ/ЗАПИСИ СЕТКИ
        V3DataArray SaveAr = new V3DataArray("Array save/load Test", DateTime.Now, 5, 6, 8.2, 11.3, new FdblVector2(VecCalculator.var2));
        filename = "binfile.bin";

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n_________________________________________ARRAY:________________________________________");
        Console.ResetColor();
        Console.WriteLine(SaveAr.ToLongString(OutputFormat));

        if(V3DataArray.SaveBinary(filename, SaveAr))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("_______________________________ARRAY SAVED SUCCESSFULLY!_______________________________\n" +
            $"                                (check {filename})\n\n");
            Console.ResetColor();
        }

        V3DataArray LoadAr = new V3DataArray(null, DateTime.Now);

        if(V3DataArray.LoadBinary(filename, ref LoadAr))
        {
            Console.WriteLine(LoadAr.ToLongString(OutputFormat));
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("_______________________________ARRAY LOADED SUCCESSFULLY!______________________________\n");
            Console.ResetColor();
        }


//.....ПРОВЕРКА ЧТЕНИЯ/ЗАПИСИ СПИСКА В НЕСУЩЕСТВУЮЩИЙ ФАЙЛ
        filename = "some-non-existent-file";

        if(V3DataList.SaveAsText(filename, SaveLst))
        { Console.WriteLine("Miracles Happen!\n"); }
    
        if(V3DataList.LoadAsText(filename, ref LoadLst))
        { Console.WriteLine("Miracles Happen!\n"); }


//.....ПРОВЕРКА ЧТЕНИЯ/ЗАПИСИ СЕТКИ В НЕСУЩЕСТВУЮЩИЙ ФАЙЛ
        if(V3DataArray.SaveBinary(filename, SaveAr))
        { Console.WriteLine("Miracles Happen!\n"); }
    
        if(V3DataArray.LoadBinary(filename, ref LoadAr))
        { Console.WriteLine("Miracles Happen!\n"); }
    }


    static void TestLINQ(string OutputFormat)
    {
//......СОЗДАНИЕ ЭЛЕМЕНТОВ ДЛЯ КОЛЛЕКЦИИ
        V3DataList l1 = new V3DataList("List Entry #1", DateTime.Now);
        l1.AddDefaults(7, new FdblVector2(VecCalculator.var1));
        V3DataList l2 = new V3DataList("List Entry #2", DateTime.Now);
        l2.AddDefaults(10, new FdblVector2(VecCalculator.var2));
        V3DataList lempty = new V3DataList("List Entry #3", DateTime.Now);

        V3DataArray a1 = new V3DataArray("Array entry #1", DateTime.Now, 6, 8, 1.5, 2.8, new FdblVector2(VecCalculator.var3));
        V3DataArray a2 = new V3DataArray("Array entry #2", DateTime.Now, 11, 17, 2.24, 5.37, new FdblVector2(VecCalculator.var1));
        V3DataArray aempty = new V3DataArray("Array entry #3", DateTime.Now);
        
//......СОЗДАНИЕ КОЛЛЕКЦИИ
        V3MainCollection Collection = new V3MainCollection();
        Collection.Add(a1);
        Collection.Add(a2);
        Collection.Add(aempty);
        Collection.Add(l1);
        Collection.Add(l2);
        Collection.Add(lempty);

//......СОЗДАНИЕ ПУСТОЙ КОЛЛЕКЦИИ
        V3MainCollection EmptyCollection = new V3MainCollection();

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n__________________________________NON-EMPTY COLLECTION:_________________________________");
        Console.ResetColor();
        
        Console.WriteLine(Collection.ToLongString(OutputFormat));
        
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("________________________________________________________________________________________\n" +
        "\n____________________________________EMPTY COLLECTION:___________________________________");
        Console.ResetColor();

        Console.WriteLine(EmptyCollection.ToLongString(OutputFormat));


//......ВЫПОЛНЕНИЕ СВОЙСТВ LINQ
//...1) СРЕДНЕЕ ЗНАЧЕНИЕ РАССТОЯНИЯ ОТ ТОЧЕК ИЗМЕРЕНИЯ ДО НАЧАЛА КООРДИНАТ
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n___________________________LINQ: AVERAGE DISTANCE CHECK:________________________________\n");
        Console.ResetColor();

//......ПРОВЕРКА НЕПУСТОЙ КОЛЛЕКЦИИ
        Console.WriteLine("\nCHECKING NON-EMPTY COLLECTON...\n");
        double Avrg = Collection.Avrg;
        if(double.IsNaN(Avrg))
            Console.WriteLine("!!!ERROR!!!\n");
        else
            Console.WriteLine($"AVERAGE MEASURING POINT-TO-ORIGIN POINT DISTANCE: {Avrg}\n");

//......ПРОВЕРКА ПУСТОЙ КОЛЛЕКЦИИ
        Console.WriteLine("\nCHECKING EMPTY COLLECTON...\n");
        Avrg = EmptyCollection.Avrg;
        if(!double.IsNaN(Avrg))
            Console.WriteLine("!!!ERROR!!!\n");
        else
            Console.WriteLine("double.NaN WAS RETURNED\n");


//...2) ПЕРЕЧИСЛЕНИЕ РАЗНОСТЕЙ МЕЖДУ МАКСИМАЛЬНЫМИ И МИНИМАЛЬНЫМИ ЗНАЧЕНИЯМИ МОДУЛЕЙ ПОЛЯ
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n________________________LINQ: MAX & MIN MODULES DIFFERENCE:_____________________________\n");
        Console.ResetColor();

//......ПРОВЕРКА НЕПУСТОЙ КОЛЛЕКЦИИ
        Console.WriteLine("\nCHECKING NON-EMPTY COLLECTON...\n");
        var ModuleDifs = Collection.Absolute;
        if(ModuleDifs == null)
            Console.WriteLine("!!!ERROR!!!\n");
        else
        {
            int n = 1;
            foreach(float i in ModuleDifs)
            {
                if(Collection[n-1].Count == 0)
                    n++;
                Console.WriteLine($"FOR COLLECTION {n}:  {i}");
                n++;
            }
            Console.WriteLine();
        }

//......ПРОВЕРКА ПУСТОЙ КОЛЛЕКЦИИ
        Console.WriteLine("\nCHECKING EMPTY COLLECTON...\n");
        ModuleDifs = EmptyCollection.Absolute;
        if(ModuleDifs != null)
            Console.WriteLine("!!!ERROR!!!\n");
        else
            Console.WriteLine("null VALUE WAS RETURNED\n");


//...3) ГРУППИРОВКА ВСЕХ РЕЗУЛЬТАТОВ ИЗМЕРЕНИЯ ПОЛЯ ПО КООРДИНАТЕ Х
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("_________________________________LINQ: GROUP BY X:______________________________________\n");
        Console.ResetColor();

//......ПРОВЕРКА НЕПУСТОЙ КОЛЛЕКЦИИ
        Console.WriteLine("\nCHECKING NON-EMPTY COLLECTON...\n");
        var ResultGroups = Collection.Grouping;
        if(ResultGroups == null)
            Console.WriteLine("!!!ERROR!!!\n");
        else
            foreach(IGrouping<double, DataItem> i in ResultGroups)
            {
                Console.WriteLine($"X = {i.Key}:");
                foreach(var j in i)
                    Console.Write($"    {j.ToLongString(OutputFormat)}");
                Console.WriteLine();
            }
        Console.WriteLine();

//......ПРОВЕРКА ПУСТОЙ КОЛЛЕКЦИИ
        Console.WriteLine("\nCHECKING EMPTY COLLECTON...\n");
        ResultGroups = EmptyCollection.Grouping;
        if(ResultGroups != null)
            Console.WriteLine("!!!ERROR!!!\n");
        else
            Console.WriteLine("null VALUE WAS RETURNED\n");

    }

    
    static void Main()
    {
        string format = "{0:F3}";

        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine("\n.......................INITIANING SAVING / LOADING DATA TESTS..........................");
        Console.ResetColor();

        SaveLoadObjects(format);

        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine("..................................TESTING COMPLETE!....................................\n");
        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine("\n\n.................................INITIATING LINQ TESTS:.................................");
        Console.ResetColor();

        TestLINQ(format);

        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine("..................................LINQ TESTS COMPLETE!..................................\n");
        Console.ResetColor();
    }
}
