#![allow(non_snake_case)]

pub mod unity;
pub mod rot_ferris;
pub mod logger;

#[repr(C)]
pub struct UniBridgeGlue {
    // エラーハンドリング用
    handle_panic: extern "C" fn (),
    error_log: extern "C" fn (message: &str),
    warn_log: extern "C" fn (message: &str),
    info_log: extern "C" fn (message: &str),

    // インスタンス生成・メソッド呼び出し
    new_instance: extern "C" fn (class_name: &str, args: &[u64]) -> u64,
    dispose_instance: extern "C" fn (id: u64),
    invoke_method: extern "C" fn (id: u64, method: &str, args: &[u64]) -> u64,
    invoke_as: extern "C" fn (id: u64, class_name: &str, method: &str, args: &[u64]) -> u64,

    // プリミティブ型 <-> オブジェクト型への変換
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
/// UniBridgeのランタイムを初期化します。
extern "C" fn unibridge_init_runtime(glue: UniBridgeGlue) {
    // グルー関数を設定
    set_glue(glue);

    // ロガーの初期化
    init_panic_handler();
    if logger::init().is_err() {
        /* ロガーが既に設定されているっぽい？ */
    }
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

        error_log(&format!("plugin panicked at '{}', {}", msg, location));

        // Unity側のパニックハンドラを呼び出す
        handle_panic();
    }));
}
