#![allow(non_snake_case)]
#![warn(clippy::all)]

pub mod logger;
pub mod rot_ferris;
pub mod unity;

mod glue;
mod instance;

pub use glue::UniBridgeGlue;
pub use instance::Instance;

#[no_mangle]
/// UniBridgeのランタイムを初期化します。
extern "C" fn unibridge_init_runtime(glue: UniBridgeGlue) {
    // グルー関数を設定
    glue::set_glue(glue);

    // ロガーの初期化
    init_panic_handler();
    if logger::init().is_err() { /* ロガーが既に設定されているっぽい？ */ }
}

#[no_mangle]
extern "C" fn unibridge_drop_runtime() {
    glue::drop_glue();
}

use unity::RustInstance;

#[no_mangle]
unsafe extern "C" fn unibridge_invoke(
    i: *mut Box<dyn RustInstance>,
    method_name: &str,
    args: &[Instance],
) -> Instance {
    use std::panic::{catch_unwind, AssertUnwindSafe};

    match catch_unwind(AssertUnwindSafe(|| (*i).invoke(method_name, args))) {
        Ok(r) => r,
        Err(_) => {
            // Unity側のパニックハンドラを呼び出す
            (glue::get_glue().handle_panic)();
            Instance::null()
        }
    }
}

fn init_panic_handler() {
    use std::panic;

    panic::set_hook(Box::new(|info| {
        // パニック情報をUnityのログに流す
        let location = info.location().unwrap();
        let msg = match info.payload().downcast_ref::<&'static str>() {
            Some(&s) => s,
            None => match info.payload().downcast_ref::<String>() {
                Some(s) => &*s,
                None => "Box<Any>",
            },
        };

        let UniBridgeGlue { ref error_log, .. } = glue::get_glue();

        error_log(&format!("plugin panicked at '{}', {}", msg, location));
    }));
}
