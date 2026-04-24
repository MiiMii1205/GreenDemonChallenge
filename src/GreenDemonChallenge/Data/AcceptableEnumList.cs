using System;
using System.Linq;
using BepInEx.Configuration;

namespace GreenDemonChallenge.Data;

public class AcceptableEnumList<T> : AcceptableValueBase where T : Enum
{
    public virtual T[] AcceptableValues { get; }
    
    public AcceptableEnumList(params T[] acceptableValues) : base(typeof(T))
    {
        if (acceptableValues == null)
            throw new ArgumentNullException(nameof(acceptableValues));
        AcceptableValues = acceptableValues.Length != 0
            ? acceptableValues
            : throw new ArgumentException("At least one acceptable value is needed", nameof(acceptableValues));

    }

    public override object Clamp(object value)
    {
        return IsValid(value) ? value : AcceptableValues[0];
    }

    public override bool IsValid(object value)
    {
        if (value is T e)
        {
            return AcceptableValues.Any<T>((Func<T, bool>) (x => x.Equals(e)));
        }

        return false;
    }

    public override string ToDescriptionString()
    {
        return "# Acceptable values: " + string.Join(", ",
            AcceptableValues.Select<T, string>((Func<T, string>) (x => x.ToString()))
            .ToArray<string>());
    }
}