namespace mprRebarTools.Enums;

/// <summary>
/// Вариант обработки элементов
/// </summary>
public enum ElementsProcessVariant
{
    /// <summary>
    /// Выбранный элемент
    /// </summary>
    SelectedElement = 0,

    /// <summary>
    /// Несколько выбранных элементов
    /// </summary>
    SelectedElements = 1,

    /// <summary>
    /// Все элементы на виде
    /// </summary>
    AllElementsOnView = 2,
        
    /// <summary>
    /// Все, кроме выбранных, элементы на виде
    /// </summary>
    AllButSelectedElementsOnView = 3
}