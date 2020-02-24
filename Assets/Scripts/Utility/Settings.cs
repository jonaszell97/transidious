
namespace Transidious
{
    public class QualitySettings
    {
        public enum QualityLevel
        {
            /// High quality settings.
            High,

            /// Medium quality settings.
            Medium,

            /// Low quality settings.
            Low,
        }

        /// The quality level.
        public QualityLevel qualityLevel;

        /// Number of corner vertices to use for streets.
        public int StreetCornerVerts;

        /// Number of cap vertices to use for streets.
        public int StreetCapVerts;

        /// Create quality settings with default presets for the given quality
        /// level.
        public QualitySettings(QualityLevel level)
        {
            switch (level)
            {
                case QualityLevel.High:
                    this.StreetCornerVerts = 5;
                    this.StreetCapVerts = 10;

                    break;
                case QualityLevel.Medium:
                    this.StreetCornerVerts = 4;
                    this.StreetCapVerts = 7;

                    break;
                case QualityLevel.Low:
                    this.StreetCornerVerts = 3;
                    this.StreetCapVerts = 6;

                    break;
            }
        }
    }

    public class Settings
    {
        /// The current settings.
        static Settings _Current;
        public static Settings Current
        {
            get
            {
                return _Current;
            }
        }

        /// The quality settings.
        public QualitySettings qualitySettings;

        /// Initialize settings with presets.
        public Settings(QualitySettings.QualityLevel qualityLevel)
        {
            this.qualitySettings = new QualitySettings(qualityLevel);

            _Current = this;
        }
    }
}