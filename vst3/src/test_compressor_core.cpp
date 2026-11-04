// Copyright (c) 2026 KGP Software Lab. SPDX-License-Identifier: MIT
#include "compressor_core.h"
#include <cstdio>
#include <cmath>
int main(){ CompressorDSP d;d.reset(48000);for(auto &b:d.bands){b.threshold=-35;b.ratio=6;b.attack=5;b.release=250;b.knee=0;b.gain=0;}d.bands[0].cutoff=120;d.bands[1].cutoff=700;d.bands[2].cutoff=3500;d.bands[3].cutoff=8500; float l[4800],r[4800],ol[4800],orr[4800];for(int i=0;i<4800;i++)l[i]=r[i]=.5f*std::sin(2*3.141592653589793*330*i/48000.);d.process(l,r,ol,orr,4800);double rms0=0,rms1=0;for(int i=0;i<4800;i++){rms0+=l[i]*l[i];rms1+=ol[i]*ol[i];}printf("input rms %.5f output rms %.5f\n",sqrt(rms0/4800),sqrt(rms1/4800));return rms1<rms0*.75 ? 0:1;}
