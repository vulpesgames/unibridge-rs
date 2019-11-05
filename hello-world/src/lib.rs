#![allow(non_snake_case)]

pub mod logger;
pub mod rot_ferris;
pub mod unity;

#[repr(C)]
pub struct Instance(u64);

impl Instance {
    pub fn null() -> Instance {
        Instance(0)
    }
}

impl Drop for Instance {
    fn drop(&mut self) {
        UniBridgeGlue::dispose_instance(Instance(self.0));
    }
}

impl From<&str> for Instance {
    fn from(s: &str) -> Instance {
        (get_glue().to_string)(s)
    }
}

#[repr(C)]
pub struct UniBridgeGlue {
    // エラーハンドリング用
    handle_panic: extern "C" fn(),
    error_log: extern "C" fn(message: &str),
    warn_log: extern "C" fn(message: &str),
    info_log: extern "C" fn(message: &str),

    // インスタンス生成・メソッド呼び出し
    new_instance: extern "C" fn(class_name: &str, args: &[Instance]) -> Instance,
    dispose_instance: extern "C" fn(id: Instance),
    invoke_method: extern "C" fn(id: Instance, method: &str, args: &[Instance]) -> Instance,
    invoke_as:
        extern "C" fn(id: Instance, class_name: &str, method: &str, args: &[Instance]) -> Instance,

    // プリミティブ型 <-> オブジェクト型への変換
    to_string: extern "C" fn(string: &str) -> Instance,
    to_f32: extern "C" fn (x: f32) -> Instance,
    try_f32: extern "C" fn (id: Instance) -> f32,
}

impl UniBridgeGlue {
    pub fn new_instance(class_name: &str, args: &[Instance]) -> Instance {
        (get_glue().new_instance)(class_name, args)
    }

    pub fn dispose_instance(id: Instance) {
        (get_glue().dispose_instance)(id)
    }

    pub fn invoke_method(id: Instance, method: &str, args: &[Instance]) -> Instance {
        (get_glue().invoke_method)(id, method, args)
    }

    pub fn invoke_as(id: Instance, class_name: &str, method: &str, args: &[Instance]) -> Instance {
        (get_glue().invoke_as)(id, class_name, method, args)
    }
}

static mut CORE_GLUE: Option<UniBridgeGlue> = None;

fn set_glue(glue: UniBridgeGlue) {
    unsafe {
        CORE_GLUE = Some(glue);
    }
}

fn get_glue() -> &'static UniBridgeGlue {
    unsafe { CORE_GLUE.as_ref().expect("UniBridgeGlue not loaded") }
}

#[no_mangle]
/// UniBridgeのランタイムを初期化します。
extern "C" fn unibridge_init_runtime(glue: UniBridgeGlue) {
    // グルー関数を設定
    set_glue(glue);

    // ロガーの初期化
    init_panic_handler();
    if logger::init().is_err() { /* ロガーが既に設定されているっぽい？ */ }

    // メソッドを呼び出してみる
    UniBridgeGlue::invoke_as(
        Instance::null(),
        "UnityEngine.Debug, UnityEngine.dll",
        "Log",
        &["Hello world!".into()],
    );
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
            },
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