pub mod mp4;

// /// A name to be specified in the CLI to edit matching tag.
// pub static NAMED_TAGS: [&'static str; 3] = [
//         "album",
//         "title",
//         "movement",
// ];

pub trait Track {}

pub struct WorkAndMovementData(
    tags::Work,
    tags::Movement,
    tags::MovementIndex,
    tags::MovementCount,
);

pub struct WorkMovementDependencies(
    tags::Title,
);

pub enum Tag {
    Album(tags::Album),
    Title(tags::Title),
    Work(tags::Work),
    Movement(tags::Movement),
    MovementIndex(tags::MovementIndex),
    MovementCount(tags::MovementCount),
}

pub mod tags {
    //! Until rust will have a type for Enum variant ...
    
    // TODO: should I create constructor that validates strings?
    pub struct Album(String);
    pub struct Title(String);
    pub struct Work(String);
    pub struct Movement(String);
    pub struct MovementIndex(u16);
    pub struct MovementCount(u16);
}

// fn extract_work_movement()
