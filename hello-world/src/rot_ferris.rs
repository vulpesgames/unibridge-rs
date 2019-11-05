use crate::unity::*;
use log::info;

pub type GameObject = Context;

pub struct RotatingFerris {
    rotation: f64,
    // #[unity(serialize_field)]
    ferris: GameObject,
}

impl MonoBehaviour for RotatingFerris {
    fn start(&mut self, _ctx: Context) {
        info!("Hello from Rust!");
    }

    fn update(&mut self, _ctx: Context) {
        self.rotation += 0.01;

        info!("ferris at: {} rad.", self.rotation);
    }
}
