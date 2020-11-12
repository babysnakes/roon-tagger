use roon_tagger::metadata::Track;

struct Tmp {}

impl Track for Tmp {}

fn main() {
    let _tmp = Tmp {};
    println!("Hello, world!");
}
