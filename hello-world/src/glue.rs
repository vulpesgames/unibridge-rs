use crate::instance::TypeCast;
use crate::Instance;

#[repr(C)]
pub struct UniBridgeGlue {
    // エラーハンドリング用
    pub(crate) handle_panic: extern "C" fn(),
    pub(crate) error_log: extern "C" fn(message: &str),
    pub(crate) warn_log: extern "C" fn(message: &str),
    pub(crate) info_log: extern "C" fn(message: &str),

    // インスタンス生成・メソッド呼び出し
    pub(crate) new_instance: extern "C" fn(class_name: &str, args: &[Instance]) -> Instance,
    pub(crate) dispose_instance: extern "C" fn(id: u64),
    pub(crate) invoke_method: extern "C" fn(id: u64, method: &str, args: &[Instance]) -> Instance,
    pub(crate) invoke_as:
        extern "C" fn(id: u64, class_name: &str, method: &str, args: &[Instance]) -> Instance,
    pub(crate) clone_instance: extern "C" fn(id: u64) -> Instance,

    pub(crate) get_property:
        extern "C" fn(id: u64, class_name: &str, property_name: &str) -> Instance,
    pub(crate) get_field:
        extern "C" fn(id: u64, class_name: &str, field_name: &str) -> Instance,

    pub(crate) set_property:
        extern "C" fn(id: u64, class_name: &str, property_name: &str, value: u64) -> bool,
    pub(crate) set_field:
        extern "C" fn(id: u64, class_name: &str, field_name: &str, value: u64) -> bool,
    // 特殊キャスト
    pub(crate) sized_bytes: extern "C" fn(ptr: &[u8]) -> Instance,

    // プリミティブ型 <-> オブジェクト型への変換
    pub(crate) to_string: extern "C" fn(string: &str) -> Instance,
    pub(crate) to_f32: extern "C" fn(x: f32) -> Instance,
    pub(crate) try_f32: extern "C" fn(id: Instance) -> TypeCast<f32>,
    pub(crate) try_bool: extern "C" fn(id: Instance) -> TypeCast<bool>,
}

impl UniBridgeGlue {
    pub fn new_instance(class_name: &str, args: &[Instance]) -> Instance {
        (get_glue().new_instance)(class_name, args)
    }

    pub fn invoke_as(id: &Instance, class_name: &str, method: &str, args: &[Instance]) -> Instance {
        (get_glue().invoke_as)(**id, class_name, method, args)
    }

    pub fn get_property(id: &Instance, class_name: &str, property_name: &str) -> Instance {
        (get_glue().get_property)(**id, class_name, property_name)
    }
}

static mut CORE_GLUE: Option<UniBridgeGlue> = None;

pub(crate) fn set_glue(glue: UniBridgeGlue) {
    unsafe {
        CORE_GLUE = Some(glue);
    }
}

pub(crate) fn glue_loaded() -> bool {
    unsafe { CORE_GLUE.is_some() }
}

pub(crate) fn get_glue() -> &'static UniBridgeGlue {
    unsafe { CORE_GLUE.as_ref().expect("UniBridgeGlue not loaded") }
}

pub(crate) fn drop_glue() {
    unsafe {
        CORE_GLUE = None;
    }
}
