model LetItBeeWater

// Depth-averaged salt transport on a regular Cartesian cell network.
// Prescribed, volume-balanced face discharges; NOT a hydrodynamic solver.
// Unity (X,Z) maps to model (X,Y). Salinity g/L equals salt mass kg / volume m3.
global {
    bool water_ready <- false;
    bool water_running <- false;
    bool water_readonly <- false;
    string water_signature <- "";
    string water_domain <- "";
    string water_epoch <- "";
    int water_revision <- 0;
    int water_width <- 0;
    int water_height <- 0;
    float water_cell_size <- 1.0;
    list water_origin <- [0.0,0.0];
    float water_surface_y <- 0.0;
    float water_diffusivity <- 0.0;
    float water_time_seconds <- 0.0;
    float water_initial_mass <- 0.0;
    float water_boundary_mass <- 0.0;
    float water_stable_dt <- 1.0;
    list<float> water_volume <- [];
    list<float> water_salt <- [];
    list water_faces <- [];
    list water_mix_faces <- [];
    list<point> water_flow_vectors <- [];
    map water_response <- ["accepted"::false,"reason"::"not_configured"];
    map water_state <- [];
    string water_checkpoint <- "";
    float water_last_saved <- -1.0;

    // Read-only presentation settings, shared by the standalone and gameplay maps.
    string live_water_file <- "";
    string water_view_status <- "Water not configured for this session";
    bool water_view_fresh <- false;
    bool water_shared_projection <- false;
    bool water_draw_cells <- true;
    float water_map_left <- 0.0;
    float water_map_top <- 0.0;
    float water_map_zoom <- 1.0;
    point water_map_offset <- {25,30,0};
    point water_display_position(int id) {
        if (water_shared_projection) {
            float x <- float(water_origin[0])+(id mod water_width+0.5)*water_cell_size;
            float y <- float(water_origin[1])+(int(id/water_width)+0.5)*water_cell_size;
            return water_map_offset+{(x-water_map_left)*water_map_zoom,(water_map_top-y)*water_map_zoom,0};
        }
        float scale <- 180.0/max(water_width,water_height);
        return {10+(id mod water_width+0.5)*scale,10+(water_height-1-int(id/water_width)+0.5)*scale};
    }
    action observe_water {
        // Only observers may read another experiment's state; they never persist it.
        if (water_readonly) {
            water_running <- false; water_view_fresh <- false;
            water_view_status <- "NO DATA | waiting for this session water checkpoint";
            try {
                if (file_exists(live_water_file)) {
                    map saved <- json_file(live_water_file).contents;
                    map snap <- map(saved["snapshot"]);
                    if (saved["schema"] = "letitbee-water-checkpoint-v1" and snap["schema"] = "letitbee-water-state-v1") {
                        do configure_water(to_json(saved["configuration"]));
                        if (bool(water_response["accepted"])) {
                            list<float> salts <- list(saved["salt_kg"]) collect float(each);
                            bool valid <- length(salts) = length(water_volume) and float(snap["time_seconds"]) >= 0
                                and snap["domain_id"] = water_domain and int(snap["width"]) = water_width and int(snap["height"]) = water_height;
                            if (valid) {
                                loop i from: 0 to: length(salts)-1 {
                                    valid <- valid and salts[i] >= 0 and salts[i] <= 100*water_volume[i]+0.000001;
                                }
                                float boundary <- float(snap["boundary_salt_kg"]);
                                valid <- valid and abs(sum(salts)-water_initial_mass-boundary) < 0.00001*max(1.0,water_initial_mass+abs(boundary));
                            }
                            if (valid) {
                                water_salt <- salts; water_time_seconds <- float(snap["time_seconds"]);
                                water_boundary_mass <- float(snap["boundary_salt_kg"]);
                                water_epoch <- string(snap["epoch"]); water_revision <- int(snap["revision"]);
                                float age <- gama.machine_time-float(saved["saved_at_ms"]);
                                water_view_fresh <- age >= 0 and age < 3000;
                                water_view_status <- (water_view_fresh ? "LIVE | " : "STALE | ")+"read only | revision "+string(water_revision);
                            } else { water_view_status <- "INVALID water snapshot | last valid field retained"; }
                        } else { water_view_status <- "Water domain changed: restart viewer"; }
                    } else { water_view_status <- "Unsupported water checkpoint"; }
                }
            } catch { water_view_status <- "Water read failed | retrying; last field retained"; }
        }
    }
    action configure_water(string water_json) {
        // Validate into temporary lists before touching the accepted simulation.
        water_response <- ["accepted"::false,"reason"::"invalid_water_configuration"];
        try {
            map c <- from_json(water_json);
            if (water_ready) {
                water_response <- ["accepted"::water_signature = water_json,
                    "reason"::(water_signature = water_json ? "already_configured" : "restart_required_for_new_water_domain")];
            } else {
                int w <- int(c["width"]); int h <- int(c["height"]);
                float dx <- float(c["cell_size_m"]); float d <- float(c["diffusivity_m2_s"]);
                list depths <- list(c["depth_m"]); list initial <- list(c["salinity_g_l"]);
                list faces <- list(c["faces"]); list origin <- list(c["origin_xz"]);
                bool valid <- c["schema"] = "letitbee-water-grid-v1" and w >= 1 and h >= 1 and w*h <= 4096
                    and dx > 0 and dx <= 1000 and d >= 0 and d <= 100
                    and length(depths) = w*h and length(initial) = w*h and length(origin) = 2
                    and length(faces) <= w*h*4 and c contains_key "domain_id" and length(string(c["domain_id"])) > 0
                    and c contains_key "surface_y" and abs(float(c["surface_y"])) < 1000000;
                if (length(origin) = 2) { valid <- valid and abs(float(origin[0])) < 1000000 and abs(float(origin[1])) < 1000000; }
                list<float> volumes <- []; list<float> salts <- []; list<float> rates <- []; list<float> balance <- [];
                list mixing <- [];
                if (valid) {
                    loop i from: 0 to: w*h-1 {
                        float depth <- float(depths[i]); float sal <- float(initial[i]);
                        valid <- valid and depth >= 0 and depth <= 1000 and sal >= 0 and sal <= 100 and (depth > 0 or sal = 0);
                        volumes << depth*dx*dx; salts << sal*depth*dx*dx; rates << 0.0; balance << 0.0;
                    }
                    loop raw over: faces {
                        map f <- map(raw); int a <- int(f["from"]); int b <- int(f["to"]);
                        float q <- float(f["discharge_m3_s"]); float s <- float(f["boundary_salinity_g_l"]);
                        bool endpoints <- a >= -1 and b >= -1 and a < w*h and b < w*h and a != b;
                        valid <- valid and endpoints and q >= 0 and q <= 100000 and s >= 0 and s <= 100;
                        if (endpoints) {
                            if (a >= 0) { valid <- valid and volumes[a] > 0; rates[a] <- rates[a]+q; balance[a] <- balance[a]-q; }
                            if (b >= 0) { valid <- valid and volumes[b] > 0; balance[b] <- balance[b]+q; }
                            if (a >= 0 and b >= 0) {
                                valid <- valid and (abs(a-b) = w or (abs(a-b) = 1 and int(a/w) = int(b/w)));
                            } else {
                                int edge <- max(a,b);
                                valid <- valid and (edge mod w = 0 or edge mod w = w-1 or edge < w or edge >= w*(h-1));
                            }
                        }
                    }
                    loop i from: 0 to: w*h-1 {
                        valid <- valid and abs(balance[i]) < 0.000001;
                        if (volumes[i] > 0) {
                            list<int> next_cells <- [];
                            if (i mod w < w-1) { next_cells << i+1; }
                            if (i+w < w*h) { next_cells << i+w; }
                            loop j over: next_cells {
                                if (volumes[j] > 0) {
                                    float k <- d*min(float(depths[i]),float(depths[j]));
                                    mixing << ["a"::i,"b"::j,"k"::k];
                                    rates[i] <- rates[i]+k; rates[j] <- rates[j]+k;
                                }
                            }
                        }
                    }
                }
                if (valid) {
                    float dt <- 1.0;
                    loop i from: 0 to: w*h-1 { if (rates[i] > 0) { dt <- min(dt,0.45*volumes[i]/rates[i]); } }
                    if (dt >= 0.001) {
                        water_volume <- volumes; water_salt <- salts; water_faces <- faces; water_mix_faces <- mixing;
                        water_width <- w; water_height <- h; water_cell_size <- dx; water_diffusivity <- d;
                        water_origin <- origin; water_surface_y <- float(c["surface_y"]);
                        water_domain <- string(c["domain_id"]); water_epoch <- string(gama.machine_time);
                        water_signature <- water_json; water_stable_dt <- dt;
                        water_initial_mass <- sum(salts); water_boundary_mass <- 0.0; water_time_seconds <- 0.0;
                        water_revision <- 0; water_ready <- true; water_running <- false;
                        if (water_checkpoint != "" and file_exists(water_checkpoint)) { do restore_water; }
                        if (water_ready) {
                            do rebuild_water_cells;
                            water_response <- ["accepted"::true,"reason"::"configured"];
                        }
                    } else { water_response <- ["accepted"::false,"reason"::"time_step_too_small"] ; }
                } else { water_response <- ["accepted"::false,"reason"::"invalid_or_unbalanced_domain"] ; }
            }
        } catch { water_response <- ["accepted"::false,"reason"::"invalid_water_configuration"]; }
    }

    action advance_water(float seconds) {
        if (water_ready and seconds > 0 and seconds <= 10) {
            int n <- int(ceil(seconds/water_stable_dt)); float dt <- seconds/n;
            loop times: n {
                list<float> delta <- water_salt collect 0.0;
                loop raw over: water_faces {
                    map f <- map(raw); int a <- int(f["from"]); int b <- int(f["to"]);
                    float sal <- a < 0 ? float(f["boundary_salinity_g_l"]) : water_salt[a]/water_volume[a];
                    float amount <- float(f["discharge_m3_s"])*dt*sal;
                    if (a >= 0) { delta[a] <- delta[a]-amount; } else { water_boundary_mass <- water_boundary_mass+amount; }
                    if (b >= 0) { delta[b] <- delta[b]+amount; } else { water_boundary_mass <- water_boundary_mass-amount; }
                }
                loop raw over: water_mix_faces {
                    map f <- map(raw); int a <- int(f["a"]); int b <- int(f["b"]);
                    float amount <- float(f["k"])*dt*(water_salt[a]/water_volume[a]-water_salt[b]/water_volume[b]);
                    delta[a] <- delta[a]-amount; delta[b] <- delta[b]+amount;
                }
                loop i from: 0 to: length(water_salt)-1 { water_salt[i] <- water_salt[i]+delta[i]; }
            }
            water_time_seconds <- water_time_seconds+seconds; water_revision <- water_revision+1;
        }
    }
    action set_water_running(bool running) { water_running <- water_ready and !water_readonly and running; }
    action read_water { water_state <- water_snapshot(); do persist_water; }
    reflex simulate_water when: water_ready and water_running and !water_readonly {
        do advance_water(float(step));
        if (water_time_seconds-water_last_saved >= 1.0) { do persist_water; }
    }
    action persist_water {
        if (water_ready and !water_readonly and water_checkpoint != "") {
            try {
                save json_file(water_checkpoint,["schema"::"letitbee-water-checkpoint-v1",
                    "configuration"::from_json(water_signature),"salt_kg"::water_salt,
                    "snapshot"::water_snapshot(),"saved_at_ms"::gama.machine_time]) rewrite: true;
                water_last_saved <- water_time_seconds;
            } catch {
                water_running <- false;
                water_response <- ["accepted"::false,"reason"::"water_checkpoint_write_failed"];
            }
        }
    }
    action restore_water {
        try {
            map saved <- json_file(water_checkpoint).contents;
            map previous <- map(saved["configuration"]); map current <- from_json(water_signature);
            list<float> restored <- list(saved["salt_kg"]) collect float(each); map snapshot <- map(saved["snapshot"]);
            bool compatible <- saved["schema"] = "letitbee-water-checkpoint-v1" and previous = current
                and length(restored) = length(water_volume) and float(snapshot["time_seconds"]) >= 0;
            if (compatible) {
                loop i from: 0 to: length(restored)-1 {
                    float mass <- float(restored[i]);
                    compatible <- compatible and mass >= 0 and mass <= 100*water_volume[i]+0.000001;
                }
            }
            if (compatible) {
                float boundary <- float(snapshot["boundary_salt_kg"]);
                compatible <- abs(sum(restored)-water_initial_mass-boundary) < 0.00001*max(1.0,water_initial_mass+abs(boundary));
            }
            if (compatible) {
                water_salt <- restored; water_time_seconds <- float(snapshot["time_seconds"]);
                water_boundary_mass <- float(snapshot["boundary_salt_kg"]); water_revision <- int(snapshot["revision"]);
                water_last_saved <- water_time_seconds;
            } else {
                water_ready <- false;
                water_response <- ["accepted"::false,"reason"::"water_checkpoint_configuration_or_state_mismatch"];
            }
        } catch {
            water_ready <- false;
            water_response <- ["accepted"::false,"reason"::"water_checkpoint_read_failed"];
        }
    }
    map water_snapshot {
        list<float> sal <- [];
        if (!empty(water_salt)) {
            loop i from: 0 to: length(water_salt)-1 { sal << (water_volume[i] > 0 ? water_salt[i]/water_volume[i] : 0.0); }
        }
        return ["schema"::"letitbee-water-state-v1","ready"::water_ready,"running"::water_running,
            "domain_id"::water_domain,"epoch"::water_epoch,"revision"::water_revision,"time_seconds"::water_time_seconds,
            "width"::water_width,"height"::water_height,"cell_size_m"::water_cell_size,"origin_xz"::water_origin,
            "surface_y"::water_surface_y,"salinity_g_l"::sal,
            "salt_mass_kg"::sum(water_salt),"boundary_salt_kg"::water_boundary_mass,
            "mass_error_kg"::sum(water_salt)-water_initial_mass-water_boundary_mass];
    }
    action rebuild_water_cells {
        ask water_cell { do die; }
        water_flow_vectors <- water_volume collect {0,0,0};
        loop raw over: water_faces {
            map f <- map(raw); int a <- int(f["from"]); int b <- int(f["to"]);
            if (a >= 0 and b >= 0) {
                point v <- {b mod water_width - a mod water_width,int(b/water_width)-int(a/water_width),0} * float(f["discharge_m3_s"]);
                water_flow_vectors[a] <- water_flow_vectors[a]+v;
                water_flow_vectors[b] <- water_flow_vectors[b]+v;
            }
        }
        loop i from: 0 to: length(water_volume)-1 {
            if (water_volume[i] > 0) { create water_cell with: [cell_id::i]; }
        }
    }
}
species water_cell {
    int cell_id;
    float salinity_g_l update: water_volume[cell_id] > 0 ? water_salt[cell_id]/water_volume[cell_id] : 0.0;
    point world_xz update: {float(water_origin[0])+(cell_id mod water_width+0.5)*water_cell_size,float(water_origin[1])+(int(cell_id/water_width)+0.5)*water_cell_size,0};
    bool fresh_data update: !water_readonly or water_view_fresh;
    aspect default {
        if (water_draw_cells) {
        // Positive Unity Z is upward on this diagnostic map.
        float scale <- water_shared_projection ? water_cell_size*water_map_zoom : 180.0/max(water_width,water_height);
        point p <- world.water_display_position(cell_id);
        float sal <- water_salt[cell_id]/water_volume[cell_id];
        draw square(scale*0.96) at: p color: (water_readonly and !water_view_fresh ? #gray : rgb(int(30+220*min(1.0,sal/35.0)),int(170-100*min(1.0,sal/35.0)),200));
        point v <- water_flow_vectors[cell_id];
        float magnitude <- sqrt(v.x*v.x+v.y*v.y);
        if (magnitude > 0) {
            point direction <- {v.x/magnitude,-v.y/magnitude};
            point side <- {-direction.y,direction.x};
            draw polygon([p+direction*scale*0.35,p-direction*scale*0.2+side*scale*0.15,p-direction*scale*0.2-side*scale*0.15]) color: #white;
        }
        }
    }
}
