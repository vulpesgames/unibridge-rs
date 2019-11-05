use crate::Instance;

pub type Context = Instance;

pub trait MonoBehaviour {
    fn start(&mut self, _ctx: Context) {}

    fn update(&mut self, _ctx: Context) {}
}

pub trait RustInstance {
    fn invoke(&mut self, _ctx: Context, _method_name: &str, _args: &[Instance]) -> Instance {
        Instance::null()
    }
}
