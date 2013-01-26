using System;
using System.Text;

namespace Symitar
{
  public class SymFile
  {
    //========================================================================
    public enum Type
    {
      REPGEN = 0,
      PWRPLS = 0,
      LETTER = 1,
      HELP   = 2,
      REPORT = 3
    }
    //========================================================================
    public static string[] TypeDescriptor = { "RepWriter", "Letter", "Help", "Report" };
    //========================================================================
    public string   server;
    public string   sym;
    public string   name;
    public DateTime date;
    public int      size;
    public Type     type;
    public int      sequence;
    public string   title;
    public int      pages;
    //========================================================================
    public SymFile()
    {
      server = sym = name = "";
      date = DateTime.Now;
      size = 0;
      type = Type.REPGEN;
    }
    //------------------------------------------------------------------------
    public SymFile(string Server, string Sym, string Name, string Date, string Time, int Size, Type typ)
    {
      server = Server;
      name   = Name;
      date   = ParseDate(Date, Time);
      sym    = Sym;
      size   = Size;
      type   = typ;
    }
    //------------------------------------------------------------------------
    public SymFile(string Server, string Sym, string Name, DateTime Date, int Size, Type typ)
    {
      server = Server;
      name   = Name;
      date   = Date;
      sym    = Sym;
      size   = Size;
      type   = typ;
    }
    //========================================================================
    public DateTime ParseDate(string Date, string Time)
    {
      while(Time.Length < 4) Time='0'+Time;
      while(Date.Length < 8) Date='0'+Date;
      return new DateTime(
        int.Parse(Date.Substring(4,4)),
        int.Parse(Date.Substring(0,2)),
        int.Parse(Date.Substring(2,2)),
        int.Parse(Time.Substring(0,2)),
        int.Parse(Time.Substring(2,2)),
        0);
    }
    //------------------------------------------------------------------------
    public string TypeString()
    {
      return TypeDescriptor[(int)type];
    }
    //------------------------------------------------------------------------
    public static string TypeString(Type type)
    {
      return TypeDescriptor[(int)type];
    }
    //========================================================================
  }
}
