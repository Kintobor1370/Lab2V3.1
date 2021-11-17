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

    public double VectorAbs()
    { return Sqrt(Pow(VecVal.X, 2) + Pow(VecVal.Y, 2)); }

    public string ToLongString(string format)
    { return "X=" + String.Format(format, X) + ";  Y=" + String.Format(format, Y) + "\n"; }

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
            abs = Vector2.Abs(Item.VecVal);
            info += Convert.ToString(n) + ")  X=" + String.Format(format, Item.X) +"   Y=" + 
            String.Format(format, Item.Y) + "   Vector Value: " + String.Format(format, Item.VecVal) + "   Vector Module: " + 
            String.Format(format, abs) +"\n";
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
        catch(FileNotFoundException e)
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
        catch(FileNotFoundException e)
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
                abs = Vector2.Abs(InfoVec[i,j]);
                str += Convert.ToString(n) + ".  X=" + String.Format(format, i*Xstep) + 
                "   Y=" + String.Format(format, j*Ystep) + "  Vector Value: " + String.Format(format, InfoVec[i, j]) + 
                "   Vector Module: " + String.Format(format, abs) + "\n";
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
        catch(FileNotFoundException e)
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
        catch(FileNotFoundException e)
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
        string str = "\n__________________________________COLLECTION ITEMS:__________________________________\n";
        foreach(V3Data Item in DataList)
            str += Item.ToLongString(format) + "\n";
        return str + "__________________________________END OF COLLECTION.__________________________________\n";
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
            if(this.DataList.Count == 0)
                return double.NaN;
            
            var DataItems = from i in DataList from j in i select j;
            var Distances = DataItems.Select(i => Sqrt(Pow(i.X, 2) + Pow(i.Y, 2)));
            
            return Distances.Average();
        }
    }

//    public IEnumerable<float> Absolute
//    {
//        get
//        {
//            if(this.DataList.Count == 0)
//                return null;
//            
//            List<V3Data> Buf = this.DataList;
//            var Modules = DataList.Select(i => i.VectorAbs().Max() - i.VectorAbs().Min());
//
//            return Modules;
//      }
//    }

    public IEnumerable<IGrouping<double, DataItem>> Grouping()
    {
        if(this.DataList.Count == 0)
            return null;

        var DataItems = from i in DataList from j in i select j;
        var XGroups = DataItems.GroupBy(i => i.X);

        return XGroups;
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
    static void SaveLoadObjects()
    {
//.....ПРОВЕРКА ЧТЕНИЯ/ЗАПИСИ СПИСКА         
        V3DataArray Temp = new V3DataArray("List save/load Test", DateTime.Now, 4, 3, 5, 2, new FdblVector2(VecCalculator.var1));
        V3DataList SaveLst = Temp;

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n_________________________________________LIST:________________________________________");
        Console.ResetColor();
        Console.WriteLine(SaveLst.ToLongString("{0:F3}"));
        
        if(V3DataList.SaveAsText("textfile.txt", SaveLst))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("________________________________LIST SAVED SUCCESSFULLY!_______________________________\n" +
            "                                (check textfile.txt)\n\n");
            Console.ResetColor();
        }

        V3DataList LoadLst = new V3DataList(null, DateTime.Now);

        if(V3DataList.LoadAsText("textfile.txt", ref LoadLst))
        {
            Console.WriteLine(LoadLst.ToLongString("{0:F3}"));
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("_______________________________LIST LOADED SUCCESSFULLY!_______________________________\n");
            Console.ResetColor();
        }


//.....ПРОВЕРКА ЧТЕНИЯ/ЗАПИСИ СЕТКИ
        V3DataArray SaveAr = new V3DataArray("Array save/load Test", DateTime.Now, 5, 6, 8.2, 11.3, new FdblVector2(VecCalculator.var2));

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n_________________________________________ARRAY:________________________________________");
        Console.ResetColor();
        Console.WriteLine(SaveAr.ToLongString("{0:F3}"));

        if(V3DataArray.SaveBinary("binfile.bin", SaveAr))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("_______________________________ARRAY SAVED SUCCESSFULLY!_______________________________\n" +
            "                                (check binfile.bin)\n\n");
            Console.ResetColor();
        }

        V3DataArray LoadAr = new V3DataArray(null, DateTime.Now);

        if(V3DataArray.LoadBinary("binfile.bin", ref LoadAr))
        {
            Console.WriteLine(LoadAr.ToLongString("{0:F3}"));
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("_______________________________ARRAY LOADED SUCCESSFULLY!______________________________\n");
            Console.ResetColor();
        }


//.....ПРОВЕРКА ЧТЕНИЯ/ЗАПИСИ СПИСКА В НЕСУЩЕСТВУЮЩИЙ ФАЙЛ
        if(V3DataList.SaveAsText("some-non-existent-file", SaveLst))
        { Console.WriteLine("Miracles Happen!\n"); }
    
        if(V3DataList.LoadAsText("some-non-existent-file", ref LoadLst))
        { Console.WriteLine("Miracles Happen!\n"); }


//.....ПРОВЕРКА ЧТЕНИЯ/ЗАПИСИ СЕТКИ В НЕСУЩЕСТВУЮЩИЙ ФАЙЛ
        if(V3DataArray.SaveBinary("some-non-existent-file", SaveAr))
        { Console.WriteLine("Miracles Happen!\n"); }
    
        if(V3DataArray.LoadBinary("some-non-existent-file", ref LoadAr))
        { Console.WriteLine("Miracles Happen!\n"); }
    }

    static void TestLINQ()
    {

    }
    
    static void Main()
    {
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine("\n..........................SAVING / LOADING DATA TESTS BEGIN!...........................");
        Console.ResetColor();

        SaveLoadObjects();

        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine("..................................TESTING COMPLETE!....................................\n");
        Console.ResetColor();
    }
}