model LetItBeeDemo

// Transport/gameplay test only. Stress is a dimensionless illustration,
// NOT a calibrated salinity model or a prediction about real bees/flowers.
global {
    geometry shape <- rectangle(40, 40);
    float step <- 0.1;
    float stress <- 0.0;
    int cargo <- 0;
    int stored <- 0;
    int visits <- 0;
    int last_event <- -1;
    init {
        create bee_agent number: 1 { location <- {20, 20, 2}; }
        create hive number: 1 { location <- {20, 20, 0}; }
        create flower number: 10 {
            flower_id <- index;
            location <- {8 + (index mod 5) * 6, 8 + int(index / 5) * 24, 2};
        }
    }
    action set_pose(float px, float py, float pz) {
        ask bee_agent {
            location <- {max(0.5, min(39.5, px)), max(0.5, min(39.5, py)), max(0.5, min(10.0, pz))};
        }
    }
    action collect(int flower_id, int event_id) {
        if (event_id > last_event) {
            last_event <- event_id;
            if (flower_id >= 0 and flower_id < length(flower) and cargo = 0) {
                flower target <- flower[flower_id];
                if ((bee_agent[0].location distance_to target.location) <= 3.0 and target.nectar >= 1.0) {
                    ask target { nectar <- nectar - 1.0; pollinated <- true; }
                    cargo <- 1;
                    visits <- visits + 1;
                }
            }
        }
    }
    action deposit(int event_id) {
        if (event_id > last_event) {
            last_event <- event_id;
            if ((bee_agent[0].location distance_to hive[0].location) <= 4.0 and cargo = 1 and stored < 5) {
                cargo <- 0;
                stored <- stored + 1;
            }
        }
    }
    action set_stress(float value) { stress <- max(0.0, min(1.0, value)); }
}
species bee_agent {
    aspect default { draw circle(0.8) color: #yellow border: #black; }
}
species hive {
    aspect default { draw square(3) color: #orange; }
}
species flower {
    int flower_id;
    float nectar <- 1.0;
    bool pollinated <- false;
    reflex regenerate { nectar <- min(1.0, nectar + 0.001 * (1.0 - stress)); }
    aspect default {
        draw circle(1.0) color: pollinated ? #green : (stress > 0.5 ? #brown : #pink);
        draw string(flower_id) color: #black size: 1;
    }
}
experiment LetItBeeLink type: gui {
    permanent { }
    float minimum_cycle_duration <- 0.1;
    parameter "Illustrative stress (not real salinity)" var: stress min: 0.0 max: 1.0;
    output {
        display LetItBeeMap type: java2D background: #lightgray {
            species hive;
            species flower;
            species bee_agent;
        }
        monitor "Carried chunk" value: cargo;
        monitor "Hive slots / 5" value: stored;
        monitor "Pollination visits" value: visits;
        monitor "Unity position" value: bee_agent[0].location;
    }
}
