use crate::glue::get_glue;
use log::{Level, LevelFilter, Metadata, Record, SetLoggerError};

struct UnityLogger;

static LOGGER: UnityLogger = UnityLogger;

pub fn init() -> Result<(), SetLoggerError> {
    log::set_logger(&LOGGER).map(|()| log::set_max_level(LevelFilter::Info))
}

impl log::Log for UnityLogger {
    fn enabled(&self, metadata: &Metadata) -> bool {
        metadata.level() <= Level::Info
    }

    fn log(&self, record: &Record) {
        let glue = get_glue();

        if self.enabled(record.metadata()) {
            match record.level() {
                Level::Error => (glue.error_log)(&format!("{}", record.args())),
                Level::Warn => (glue.warn_log)(&format!("{}", record.args())),
                _ => (glue.info_log)(&format!("{}", record.args())),
            }
        }
    }

    fn flush(&self) {}
}
