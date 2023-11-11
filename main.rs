extern crate winapi;

use std::thread::sleep;
use std::time::Duration;

use clap::Parser;

#[derive(Parser, Debug)]
#[command(author, version, about, long_about = None)]
struct Args {
    #[arg(short, long)]
    resolution: String,

    #[arg(short, long, default_value_t = 1)]
    count: u32,
}

#[link(name = "ntdll")]
extern "system" {
    fn NtSetTimerResolution(
        desired_resolution: u32,
        set_resolution: bool,
        current_resolution: *mut u32
    ) -> u32;
    fn NtQueryTimerResolution(
        minimum_resolution: *mut u32,
        maximum_resolution: *mut u32,
        current_resolution: *mut u32
    ) -> u32;
}



fn main() {
    let args = Args::parse();
    let desired_resolution = args.resolution.parse::<u32>().unwrap();
    unsafe {
        let mut min_res: u32 = 0;
        let mut max_res: u32 = 0;
        let mut current_resolution: u32 = 0;

        loop {
            let status = NtQueryTimerResolution(&mut min_res, &mut max_res, &mut current_resolution);
            if status != 0 {
                eprintln!("NtQueryTimerResolution failed with status: {}", status);
                return;
            }
            NtSetTimerResolution(desired_resolution, true, &mut current_resolution);

            println!("Minimum Resolution: {}", min_res);
            println!("Maximum Resolution: {}", max_res);
            println!("Current Resolution: {}", current_resolution);

            sleep(Duration::from_millis(1000));
        }
    }
}

