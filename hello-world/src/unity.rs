
pub struct Context(u64);

pub trait MonoBehaviour {
    fn start(&mut self, ctx: Context) {}

    fn update(&mut self, ctx: Context) {}
}
