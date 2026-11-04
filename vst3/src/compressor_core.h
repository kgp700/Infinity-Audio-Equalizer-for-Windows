// Copyright (c) 2026 KGP Software Lab. SPDX-License-Identifier: MIT
#pragma once
// Copyright 2026 Infinity Audio Equalizer Pro contributors; MIT.
// Standalone dynamics DSP: no VST2 ABI, no Steinberg headers.
#include <cmath>
#include <algorithm>
struct BandDSP { float cutoff=20000, threshold=-22, ratio=1.5f, attack=40, release=180, knee=6, gain=0; };
struct CompressorDSP {
    BandDSP bands[5];
    float lowpass[4][2]={}, env[5]={};
    double sampleRate=48000;
    void reset(double rate) {sampleRate=rate; for(auto &v:lowpass) for(float &w:v)w=0;for(float &v:env)v=0;}
    template<typename T> void process(const T* l,const T* r,T* ol,T* orr,int count){
        float a[4], attack[5], release[5];
        for(int j=0;j<4;j++) {float fc=std::clamp(bands[j].cutoff,30.f,float(sampleRate*.45));a[j]=6.283185307f*fc/(float(sampleRate)+6.283185307f*fc);}
        for(int j=0;j<5;j++) {attack[j]=1.f/(1.f+float(sampleRate)*bands[j].attack*.001f);release[j]=1.f/(1.f+float(sampleRate)*bands[j].release*.001f);}
        for(int i=0;i<count;i++){
            float x[2]={l?float(l[i]):0.f,r?float(r[i]):0.f};
            float lp[4][2];
            for(int j=0;j<4;j++)for(int c=0;c<2;c++){lowpass[j][c]+=a[j]*(x[c]-lowpass[j][c]);lp[j][c]=lowpass[j][c];}
            float y[2]={};
            for(int j=0;j<5;j++){
                float v[2];for(int c=0;c<2;c++)v[c]=j==0?lp[0][c]:j==4?x[c]-lp[3][c]:lp[j][c]-lp[j-1][c];
                float peak=std::max(std::abs(v[0]),std::abs(v[1]));
                float coeff=peak>env[j]?attack[j]:release[j];env[j]+=coeff*(peak-env[j]);
                float over=20.f*std::log10(std::max(env[j],1.e-9f))-bands[j].threshold;
                float ratio=std::clamp(bands[j].ratio,1.f,6.f), knee=std::max(0.f,bands[j].knee), reduction=0;
                if(knee>.01f){float half=knee*.5f;if(over>-half&&over<half){float d=over+half;reduction=(1.f-1.f/ratio)*d*d/(2*knee);}else if(over>=half)reduction=(1.f-1.f/ratio)*over;}
                else if(over>0)reduction=(1.f-1.f/ratio)*over;
                float gain=std::pow(10.f,(bands[j].gain-reduction)/20.f);
                for(int c=0;c<2;c++)y[c]+=v[c]*gain;
            }
            ol[i]=T(y[0]);orr[i]=T(y[1]);
        }
    }
};
