using System;
using System.Text;

namespace Symitar
{
  public class RepRunErr
  {
    //========================================================================
    public enum Status
    {
      OKAY      = 0,
      CANCELLED = 1,
      ERRORED   = 2,
      NOTFOUND  = 3
    }
    //========================================================================
    public Status code     = Status.CANCELLED;
    public int    sequence = -1;
    public int    time     = -1;
    public string err      = null;
    //========================================================================
    public static RepRunErr Okay(int sequence, int time)
    {
      RepRunErr ret = new RepRunErr();
      ret.code = Status.OKAY;
      ret.sequence = sequence;
      ret.time     = time;
      return ret;
    }
    //------------------------------------------------------------------------
    public static RepRunErr Cancelled()
    {
      return new RepRunErr();
    }
    //------------------------------------------------------------------------
    public static RepRunErr Errored(string error)
    {
      RepRunErr ret = new RepRunErr();
      ret.code = Status.ERRORED;
      ret.err = error;
      return ret;
    }
    //------------------------------------------------------------------------
    public static RepRunErr NotFound()
    {
      RepRunErr ret = new RepRunErr();
      ret.code = Status.NOTFOUND;
      return ret;
    }
    //========================================================================
  }
}
