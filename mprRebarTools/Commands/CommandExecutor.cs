namespace mprRebarTools.Commands
{
    using System;
    using Autodesk.Revit.UI;
    using ModPlusAPI.Windows;

    /// <summary>
    /// Исполнитель команд
    /// </summary>
    public class CommandExecutor
    {
        /// <summary>
        /// Выполнить команду
        /// </summary>
        /// <param name="action">Выполняемая команда</param>
        public static Result Execute(Action action)
        {
            try
            {
                action.Invoke();
                return Result.Succeeded;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
                return Result.Failed;
            }
        }
    }
}
