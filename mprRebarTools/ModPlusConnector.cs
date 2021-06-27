namespace mprRebarTools
{
    using System;
    using System.Collections.Generic;
    using ModPlusAPI.Abstractions;
    using ModPlusAPI.Enums;

    /// <inheritdoc/>
    public class ModPlusConnector : IModPlusPlugin
    {
        private static ModPlusConnector _instance;

        /// <summary>
        /// Singleton instance
        /// </summary>
        public static ModPlusConnector Instance => _instance ?? (_instance = new ModPlusConnector());
        
        /// <inheritdoc/>
        public SupportedProduct SupportedProduct => SupportedProduct.Revit;

        /// <inheritdoc/>
        public string Name => nameof(mprRebarTools);

#if R2017
        /// <inheritdoc/>
        public string AvailProductExternalVersion => "2017";
#elif R2018
        /// <inheritdoc/>
        public string AvailProductExternalVersion => "2018";
#elif R2019
        /// <inheritdoc/>
        public string AvailProductExternalVersion => "2019";
#elif R2020
        /// <inheritdoc/>
        public string AvailProductExternalVersion => "2020";
#elif R2021
        /// <inheritdoc/>
        public string AvailProductExternalVersion => "2021";
#elif R2022
        /// <inheritdoc/>
        public string AvailProductExternalVersion => "2022";
#endif

        /// <inheritdoc/>
        public string FullClassName => string.Empty;

        /// <inheritdoc/>
        public string AppFullClassName => $"{nameof(mprRebarTools)}.App";

        /// <inheritdoc/>
        public Guid AddInId => Guid.Parse("45d860ed-19e9-44a9-8367-a1e7cf485cd8");

        /// <inheritdoc/>
        public string LName => "Утилиты работы с арматурой";

        /// <inheritdoc/>
        public string Description => "Сборник небольших вспомогательных плагинов для работы с арматурой";

        /// <inheritdoc/>
        public string Author => "Александр Пекшев";

        /// <inheritdoc/>
        public string Price => "0";

        /// <inheritdoc/>
        public bool CanAddToRibbon => false;

        /// <inheritdoc/>
        public string FullDescription => string.Empty;

        /// <inheritdoc/>
        public string ToolTipHelpImage => string.Empty;

        /// <inheritdoc/>
        public List<string> SubPluginsNames => new List<string>();
        
        /// <inheritdoc/>
        public List<string> SubPluginsLNames => new List<string>();

        /// <inheritdoc/>
        public List<string> SubDescriptions => new List<string>();

        /// <inheritdoc/>
        public List<string> SubFullDescriptions => new List<string>();

        /// <inheritdoc/>
        public List<string> SubHelpImages => new List<string>();

        /// <inheritdoc/>
        public List<string> SubClassNames => new List<string>();
    }
}
