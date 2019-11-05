#![allow(non_snake_case)]

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

use unity::{Context, RustInstance};

#[no_mangle]
unsafe extern "C" fn unibridge_invoke(
    i: *mut Box<dyn RustInstance>,
    ctx: Context,
    method_name: &str,
    args: &[Instance],
) -> Instance {
    (*i).invoke(ctx, method_name, args)
}

fn init_panic_handler() {
    use std::panic;

    panic::set_hook(Box::new(|info| {
        // パニック情報をUnityのログに流す
        let location = info.location().unwrap();
        let msg = match info.payload().downcast_ref::<&'static str>() {
            Some(s) => *s,
            None => match info.payload().downcast_ref::<String>() {
                Some(s) => &s[..],
                None => "Box<Any>",
            },
        };

        let UniBridgeGlue {
            ref handle_panic,
            ref error_log,
            ..
        } = glue::get_glue();

        error_log(&format!("plugin panicked at '{}', {}", msg, location));

        // Unity側のパニックハンドラを呼び出す
        handle_panic();
    }));
}
