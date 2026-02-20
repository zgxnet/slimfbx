namespace SlimFbx;

public struct FbxTime
{
    public long Value;

    public const long TC_MILLISECOND = 141120;
    public const long TC_SECOND = TC_MILLISECOND * 1000;
    public const long TC_MINUTE = TC_SECOND * 60;
    public const long TC_HOUR = TC_MINUTE * 60;
    public const long TC_DAY = TC_HOUR * 24;

    public const double TimeUnit = 1.0 / TC_SECOND; //46186158000

    /** Time modes.
  * \remarks
  * EMode \c eNTSCDropFrame is used for broadcasting operations where 
  * clock time must be (almost) in sync with time code. To bring back color 
  * NTSC time code with clock time, this mode drops 2 frames per minute
  * except for every 10 minutes (00, 10, 20, 30, 40, 50). 108 frames are 
  * dropped per hour. Over 24 hours the error is 2 frames and 1/4 of a 
  * frame. A time-code of 01:00:03:18 equals a clock time of 01:00:00:00
  * 
  * \par
  * EMode \c eNTSCFullFrame represents a time address and therefore is NOT 
  * IN SYNC with clock time. A time code of 01:00:00:00 equals a clock time 
  * of 01:00:03:18.
  * 
  * - \e eDefaultMode		
  * - \e eFrames120			120 frames/s
  * - \e eFrames100			100 frames/s
  * - \e eFrames60          60 frames/s
  * - \e eFrames50          50 frames/s
  * - \e eFrames48          48 frame/s
  * - \e eFrames30          30 frames/s (black and white NTSC)
  * - \e eFrames30Drop		Kept for legacy reasons. Being equivalent to NTSC drop, use eNTSCDropFrame instead.
  * - \e eNTSCDropFrame		~29.97 frames/s drop color NTSC
  * - \e eNTSCFullFrame		~29.97 frames/s color NTSC
  * - \e ePAL				25 frames/s	PAL/SECAM
  * - \e eFrames24			24 frames/s Film/Cinema
  * - \e eFrames1000		1000 milli/s (use for date time)
  * - \e eFilmFullFrame		~23.976 frames/s
  * - \e eCustom            Custom frame rate value
  * - \e eFrames96			96 frames/s
  * - \e eFrames72			72 frames/s
  * - \e eFrames59dot94		~59.94 frames/s
  * - \e eFrames119dot88	~119.88 frames/s
  * - \e eModesCount		Number of time modes
  */
    public enum EMode
    {
        DefaultMode,
        Frames120,
        Frames100,
        Frames60,
        Frames50,
        Frames48,
        Frames30,
        Frames30Drop,
        NTSCDropFrame,
        NTSCFullFrame,
        PAL,
        Frames24,
        Frames1000,
        FilmFullFrame,
        Custom,
        Frames96,
        Frames72,
        Frames59dot94,
        Frames119dot88
    }

    static long[] OneFrameValue = [
        4704000,
        1176000,
        1411200,
        2352000,
        2822400,
        2940000,
        4704000,
        0,
        4708704,
        4708704,
        5644800,
        5880000,
        141120,
        5885880,
        11289600,
        1470000,
        1960000,
        2354352,
        1177176
    ];

    public FbxTime()
    { }

    public FbxTime(long value)
    {
        Value = value;
    }

    public readonly float FloatTime => (float)(Value * TimeUnit);

    public static long GetOneFrameValue(EMode mode)
        => OneFrameValue[(int)mode];

    public static implicit operator FbxTime(long value)
        => new (value);

    public static implicit operator long(FbxTime time)
        => time.Value;
}
