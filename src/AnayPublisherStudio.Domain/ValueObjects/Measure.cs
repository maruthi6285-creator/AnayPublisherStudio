namespace AnayPublisherStudio.Domain.ValueObjects;

/// <summary>
/// An immutable physical measurement expressed in inches, with helpers to
/// convert to the units required by rendering back-ends (points, millimetres).
/// </summary>
public readonly record struct Measure(double Inches)
{
    /// <summary>PostScript points (72 per inch), used by PDF engines.</summary>
    public double Points => Inches * 72.0;

    /// <summary>Millimetres equivalent.</summary>
    public double Millimetres => Inches * 25.4;

    /// <summary>Creates a measure from millimetres.</summary>
    public static Measure FromMm(double mm) => new(mm / 25.4);

    /// <summary>Creates a measure from points.</summary>
    public static Measure FromPoints(double pt) => new(pt / 72.0);

    /// <summary>Returns a culture-invariant display string for the measure.</summary>
    public override string ToString() => $"{Inches:0.###}in ({Millimetres:0.##}mm)";
}
