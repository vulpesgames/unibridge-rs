use crate::unity::*;
use crate::Instance;
use log::info;

pub type GameObject = Context;

pub struct RotFerris {
    rotation: f32,
    // #[unity(serialize_field)]
    _ferris: GameObject,
}

#[no_mangle]
unsafe extern "C" fn new_ferris() -> *mut Box<dyn RustInstance> {
    let ferris = Box::new(RotFerris {
        rotation: 0.0,
        _ferris: Instance::null(),
    }) as Box<dyn RustInstance>;

    Box::leak(Box::new(ferris))
}

#[no_mangle]
unsafe extern "C" fn kill_ferris(ptr: *mut Box<dyn RustInstance>) {
    drop(Box::from_raw(ptr))
}

impl MonoBehaviour for RotFerris {
    fn start(&mut self, _ctx: Context) {
        info!("仙狐さんもふりたいじゃん！！！！");

        use std::convert::TryFrom;
        let pi_instance = f32::try_from(Instance::from(std::f32::consts::PI));

        if Ok(std::f32::consts::PI) == pi_instance {
            info!("Cast successful!");
        } else {
            info!("Cast failed");
        }
    }

    fn update(&mut self, ctx: Context) {
        self.rotation += 1.0;
        self.rotation %= 360.0;

        ctx.invoke("SetFerrisRotation", &[self.rotation.into()]);
    }
}

impl RustInstance for RotFerris {
    fn invoke(&mut self, ctx: Context, method_name: &str, _args: &[Instance]) -> Instance {
        match method_name {
            "Start" => {
                self.start(ctx);
                Instance::null()
            }
            "Update" => {
                self.update(ctx);
                Instance::null()
            }
            _ => Instance::null(),
        }
    }
}
