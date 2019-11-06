use crate::Instance;

pub type Context = Instance;

pub trait MonoBehaviour {
    fn start(&mut self) {}

    fn update(&mut self) {}
}

pub trait RustInstance {
    fn invoke(&mut self, _method_name: &str, _args: &[Instance]) -> Instance {
        Instance::null()
    }
}

pub struct Time;

impl Time {
    pub fn delta_time() -> f32 {
        use crate::glue::UniBridgeGlue;
        use std::convert::TryInto;

        UniBridgeGlue::get_property(
            &Instance::null(),
            "UnityEngine.Time, UnityEngine.dll",
            "deltaTime",
        )
        .try_into()
        .unwrap()
    }
}
