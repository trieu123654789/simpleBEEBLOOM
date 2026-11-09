model LetItBeeWaterReview
import "LetItBeeWater.gaml"
global {
    geometry shape <- rectangle(200,200);
    float step <- 0.1;
    string water_config_file <- "../Water/channel-demo.json";
    init {
        water_readonly <- live_water_file != "";
        if (water_readonly) { do observe_water; }
        else { do configure_water(water_config_file != "" ? to_json(json_file(water_config_file).contents) : "{}"); }
    }
    reflex refresh_water_observer when: water_readonly and cycle mod 2 = 0 { do observe_water; }

}
experiment WaterReview type: gui {
    parameter "Water configuration JSON" var: water_config_file;
    parameter "Live water checkpoint (optional, read only)" var: live_water_file;
    parameter "Run salt transport" var: water_running;
    output {
        display WaterGrid type: 2d { species water_cell; }
        monitor "Configuration" value: water_response;
        monitor "View status" value: water_view_status;
        monitor "Simulation seconds" value: water_time_seconds;
        monitor "Salt mass (kg)" value: sum(water_salt);
        monitor "Boundary salt exchange (kg)" value: water_boundary_mass;
        monitor "Conservation error (kg)" value: sum(water_salt)-water_initial_mass-water_boundary_mass;
    }
}
