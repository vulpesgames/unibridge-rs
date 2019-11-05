#![allow(non_snake_case)]

pub mod unity;
pub mod rot_ferris;
pub mod logger;

use log::{info, error, warn};

#[repr(C)]
pub struct UniBridgeGlue {
    // エラーハンドリング用
    handle_panic: extern "C" fn (),
    error_log: extern "C" fn (&str),
    warn_log: extern "C" fn (&str),
    info_log: extern "C" fn (&str),
}

static mut CORE_GLUE: Option<UniBridgeGlue> = None;

fn set_glue(glue: UniBridgeGlue) {
    unsafe {
        CORE_GLUE = Some(glue);
    }
}

fn get_glue() -> &'static UniBridgeGlue {
    unsafe {
        CORE_GLUE.as_ref().expect("UniBridgeGlue not loaded")
    }
}

#[no_mangle]
extern "C" fn unibridge_init_runtime(glue: UniBridgeGlue) {
    // グルー関数を設定
    set_glue(glue);

    // UniBridgeのランタイムを初期化する
    // ロガーの初期化
    init_panic_handler();
    if logger::init().is_err() {
        // error!("failed to set logger");
    }
    
    info!("Hello, world!");
    error!("this is error");
    warn!("warning: this senko is super mofumofu!");
}

#[no_mangle]
extern "C" fn unibridge_drop_runtime() {
    unsafe {
        CORE_GLUE = None;
    }
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
            }
        };
        
        let UniBridgeGlue {
            ref handle_panic,
            ref error_log,
            ..
        } = get_glue();

        error_log(&format!("glue panicked at '{}', {}", msg, location));

        // Unity側のパニックハンドラを呼び出す
        handle_panic();
    }));
}
