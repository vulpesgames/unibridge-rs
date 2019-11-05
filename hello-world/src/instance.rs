use crate::glue::get_glue;

#[repr(C)]
pub struct Instance(u64);

#[repr(C)]
pub struct TypeCast<T>(pub(crate) bool, pub(crate) T);

impl Instance {
    pub fn null() -> Instance {
        Instance(0)
    }

    pub fn is_null(&self) -> bool {
        self.0 == 0
    }

    pub fn invoke(&self, method: &str, args: &[Instance]) -> Instance {
        (get_glue().invoke_method)(self.0, method, args)
    }

    pub fn invoke_as(&self, class_name: &str, method: &str, args: &[Instance]) -> Instance {
        (get_glue().invoke_as)(self.0, class_name, method, args)
    }

    pub fn leak(instance: Self) -> u64 {
        let mut self_ = instance;
        std::mem::replace(&mut self_.0, 0)
    }
}

impl Clone for Instance {
    fn clone(&self) -> Self {
        (get_glue().clone_instance)(self.0)
    }
}

impl Default for Instance {
    fn default() -> Self {
        Self::null()
    }
}

impl Drop for Instance {
    fn drop(&mut self) {
        if self.is_null() {
            return;
        }

        (get_glue().dispose_instance)(self.0);
    }
}

impl From<&str> for Instance {
    fn from(s: &str) -> Instance {
        (get_glue().to_string)(s)
    }
}

impl From<f32> for Instance {
    fn from(s: f32) -> Instance {
        (get_glue().to_f32)(s)
    }
}

use std::convert::TryFrom;

#[derive(Clone, Copy, PartialEq, Eq)]
pub struct CastError;

impl TryFrom<Instance> for f32 {
    type Error = CastError;

    fn try_from(id: Instance) -> Result<Self, Self::Error> {
        let TypeCast(succ, val) = (get_glue().try_f32)(id);

        if succ {
            Ok(val)
        } else {
            Err(CastError)
        }
    }
}

impl TryFrom<Instance> for bool {
    type Error = CastError;

    fn try_from(id: Instance) -> Result<Self, Self::Error> {
        if id.is_null() {
            return Ok(false);
        }

        let TypeCast(succ, val) = (get_glue().try_bool)(id);

        if succ {
            Ok(val)
        } else {
            Err(CastError)
        }
    }
}

impl std::ops::Deref for Instance {
    type Target = u64;

    fn deref(&self) -> &Self::Target {
        &self.0
    }
}
