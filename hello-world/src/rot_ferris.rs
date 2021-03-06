use crate::math::*;
use crate::unity::*;

use crate::Instance;
use log::info;

pub struct RotFerris {
    // #[unity(self)]
    ctx: Context,
    rotation: f32,
    // #[unity(serialize_field)]
    ferris: Instance,
    fps_field: Instance,
}

#[no_mangle]
unsafe extern "C" fn new_ferris(ctx: Context) -> *mut Box<dyn RustInstance> {
    let ferris = Box::new(RotFerris {
        ferris: ctx.get_field("ferris"),
        fps_field: ctx.get_field("fpsField"),
        rotation: 0.0,
        ctx,
    }) as Box<dyn RustInstance>;

    Box::leak(Box::new(ferris))
}

#[no_mangle]
unsafe extern "C" fn kill_ferris(ptr: *mut Box<dyn RustInstance>) {
    drop(Box::from_raw(ptr))
}

impl MonoBehaviour for RotFerris {
    fn start(&mut self) {
        info!("仙狐さんもふりたいじゃん！！！！");

        if self.ferris.is_null() {
            info!("Ferris is null!");
        }

        if self.fps_field.is_null() {
            info!("fps field is null!");
        }

        use std::convert::TryFrom;
        let pi_instance = f32::try_from(Instance::from(std::f32::consts::PI));

        if Ok(std::f32::consts::PI) == pi_instance {
            info!("Cast successful!");
        } else {
            info!("Cast failed");
        }

        self.ctx
            .invoke("TestVector", &[Vector3::new(1.0, 2.0, 3.0).into()]);
    }

    fn update(&mut self) {
        let dt = Time::delta_time();

        self.rotation += dt * 180.0;
        self.rotation %= 360.0;

        self.ctx
            .get_property("transform")
            .set_property("rotation", &Quaternion::euler(0.0, 0.0, self.rotation).into());
        
        self.fps_field
            .set_property("text", &(&*format!("{} FPS", 1.0 / dt)).into());
    }
}

impl RustInstance for RotFerris {
    fn invoke(&mut self, method_name: &str, _args: &[Instance]) -> Instance {
        match method_name {
            "Start" => {
                self.start();
                Instance::null()
            }
            "Update" => {
                self.update();
                Instance::null()
            }
            _ => Instance::null(),
        }
    }
}
