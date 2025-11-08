using UnityEngine;

/// <summary>
/// Interface for any object that can provide tooltip information
/// </summary>
public interface ITooltipProvider
{
    string GetTooltipHeader();
    string GetTooltipDescription();
}
