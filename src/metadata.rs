pub mod mp4;

pub trait Track {}

pub trait ModifiedTrack {}

pub struct WorkAndMovementData(
    tags::Work,
    tags::Movement,
    tags::MovementIndex,
    tags::MovementCount,
);

pub trait Tag<T> {
    fn value(&self) -> &T;
}

pub mod tags {
    use super::Tag;

    pub struct Album(String);
    pub struct Title(String);
    pub struct Work(String);
    pub struct Movement(String);
    pub struct MovementIndex(u16);
    pub struct MovementCount(u16);

    /// Avoid repeatable Tag implementation for every tag.
    macro_rules! impl_tag {
        ($tag:ty, $tipe:ty) => {
            impl Tag<$tipe> for $tag {
                fn value(&self) -> &$tipe {
                    &self.0
                }
            }
        };
    }

    impl_tag! { Album, String }
    impl_tag! { Title, String }
    impl_tag! { Work, String }
    impl_tag! { Movement, String }
    impl_tag! { MovementIndex, u16 }
    impl_tag! { MovementCount, u16 }
}
