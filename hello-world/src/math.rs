pub use crate::{Instance, UniBridgeGlue};
pub use cgmath;

pub type Vector = cgmath::Vector1<f32>;
pub type Vector2 = cgmath::Vector2<f32>;
pub type Vector3 = cgmath::Vector3<f32>;
pub type Vector4 = cgmath::Vector4<f32>;
pub type Quaternion = cgmath::Quaternion<f32>;

pub type Matrix2 = cgmath::Matrix2<f32>;
pub type Matrix3 = cgmath::Matrix3<f32>;
pub type Matrix4 = cgmath::Matrix4<f32>;

pub trait EulerQuaternion {
    fn euler(x: f32, y: f32, z: f32) -> Self;
}

impl EulerQuaternion for Quaternion {
    fn euler(x: f32, y: f32, z: f32) -> Self {
        cgmath::Euler::new(cgmath::Deg(x), cgmath::Deg(y), cgmath::Deg(z)).into()
    }
}

impl From<Vector2> for Instance {
    fn from(v: Vector2) -> Instance {
        UniBridgeGlue::new_instance(
            "UnityEngine.Vector2, UnityEngine.dll",
            &[v.x.into(), v.y.into()],
        )
    }
}

impl From<Vector3> for Instance {
    fn from(v: Vector3) -> Instance {
        UniBridgeGlue::new_instance(
            "UnityEngine.Vector3, UnityEngine.dll",
            &[v.x.into(), v.y.into(), v.z.into()],
        )
    }
}

impl From<Quaternion> for Instance {
    fn from(v: Quaternion) -> Instance {
        UniBridgeGlue::new_instance(
            "UnityEngine.Quaternion, UnityEngine.dll",
            &[v.v.x.into(), v.v.y.into(), v.v.z.into(), v.s.into()],
        )
    }
}

impl From<Matrix4> for Instance {
    fn from(v: Matrix4) -> Instance {
        UniBridgeGlue::new_instance(
            "UnityEngine.Matrix4x4, UnityEngine.dll",
            &[
                v.x.x.into(),
                v.x.y.into(),
                v.x.z.into(),
                v.x.w.into(),
                v.y.x.into(),
                v.y.y.into(),
                v.y.z.into(),
                v.y.w.into(),
                v.z.x.into(),
                v.z.y.into(),
                v.z.z.into(),
                v.z.w.into(),
                v.w.x.into(),
                v.w.y.into(),
                v.w.z.into(),
                v.w.w.into(),
            ],
        )
    }
}
