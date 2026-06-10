using System;

[Serializable]
public class RuntimeStat
{
    public StatDefinition definition;

    public int currentValue;

    /// <summary>
    /// Cantidad total de veces que esta stat ha sido mejorada.
    /// Sirve para calcular la penalización escalable.
    /// </summary>
    public int totalInvestments;
}