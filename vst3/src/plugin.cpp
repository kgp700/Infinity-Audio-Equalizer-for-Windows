// Copyright (c) 2026 KGP Software Lab. SPDX-License-Identifier: MIT
// Copyright 2026 Infinity Audio Equalizer Pro contributors. MIT.
// VST3 implementation using Steinberg's MIT-licensed VST3 SDK.
#include "public.sdk/source/vst/vstaudioeffect.h"
#include "public.sdk/source/vst/vsteditcontroller.h"
#include "public.sdk/source/main/pluginfactory.h"
#include "pluginterfaces/vst/ivstparameterchanges.h"
#include "base/source/fstreamer.h"
#include "compressor_core.h"
#include <algorithm>
#include <cmath>
#include <cstring>
#include <cstdio>
#include "pluginterfaces/base/ustring.h"
#define FULL_VERSION_STR "0.7.4"
using namespace Steinberg;
using namespace Steinberg::Vst;
static const FUID kProcessor(0x9F8579E1,0xDF6B4520,0xB04C6DE1,0x0D628571);
static const FUID kController(0xA55F0C94,0x04104C50,0x9B5B5B28,0x3FAB40CC);
constexpr int kParams=34;
static constexpr const char* names[6]={"T","R","A","D","K","G"};
static float normal(float x){return std::clamp(x,0.f,1.f);}
static float physical(int p,float n){int k=p%6; if(p>=30)return 30+19970*n;switch(k){case 0:return -48+48*n;case 1:return 1+5*n;case 2:return 2+98*n;case 3:return 40+460*n;case 4:return 12*n;default:return -6+9*n;}}
static void applyParameter(CompressorDSP &d,int id,float n){ if(id>=30&&id<34){d.bands[id-30].cutoff=physical(id,n);return;} if(id<0||id>=30)return;auto &b=d.bands[id/6];switch(id%6){case 0:b.threshold=physical(id,n);break;case 1:b.ratio=physical(id,n);break;case 2:b.attack=physical(id,n);break;case 3:b.release=physical(id,n);break;case 4:b.knee=physical(id,n);break;case 5:b.gain=physical(id,n);break;}}
class Processor:public AudioEffect{
    CompressorDSP dsp; float params[kParams]{};
public:
    Processor(){setControllerClass(kController);for(int i=0;i<kParams;i++)params[i]=i<30?(i%6==0?.625f:i%6==1?.10f:i%6==2?.30f:i%6==3?.30f:i%6==4?.5f:.66666667f):((i==30?160.f:i==31?800.f:i==32?4000.f:8500.f)-30)/19970.f;for(int i=0;i<kParams;i++)applyParameter(dsp,i,params[i]);}
    static FUnknown* createInstance(void*){return (IAudioProcessor*)new Processor;}
    tresult PLUGIN_API initialize(FUnknown* context) override {auto r=AudioEffect::initialize(context);if(r!=kResultOk)return r;addAudioInput(STR16("Stereo In"),SpeakerArr::kStereo);addAudioOutput(STR16("Stereo Out"),SpeakerArr::kStereo);return kResultOk;}
    tresult PLUGIN_API setBusArrangements(SpeakerArrangement* ins,int32 ni,SpeakerArrangement* outs,int32 no) override {return ni==1&&no==1&&ins[0]==SpeakerArr::kStereo&&outs[0]==SpeakerArr::kStereo?kResultTrue:kResultFalse;}
    tresult PLUGIN_API canProcessSampleSize(int32 v) override {return (v==kSample32||v==kSample64)?kResultTrue:kResultFalse;}
    tresult PLUGIN_API setupProcessing(ProcessSetup& s) override {dsp.reset(s.sampleRate);return AudioEffect::setupProcessing(s);}
    tresult PLUGIN_API setState(IBStream* stream) override {IBStreamer read(stream,kLittleEndian);for(int i=0;i<kParams;i++){float v=0;if(!read.readFloat(v))return kResultFalse;params[i]=normal(v);applyParameter(dsp,i,params[i]);}return kResultOk;}
    tresult PLUGIN_API getState(IBStream* stream) override {IBStreamer write(stream,kLittleEndian);for(float v:params)if(!write.writeFloat(v))return kResultFalse;return kResultOk;}
    tresult PLUGIN_API process(ProcessData& data) override {
        if(data.inputParameterChanges)for(int32 i=0;i<data.inputParameterChanges->getParameterCount();i++){
            auto *q=data.inputParameterChanges->getParameterData(i);if(!q)continue;
            auto id=q->getParameterId();int32 count=q->getPointCount(),offset=0;ParamValue value=0;
            if(id<kParams&&count>0&&q->getPoint(count-1,offset,value)==kResultOk){params[id]=normal(float(value));applyParameter(dsp,int(id),params[id]);}
        }
        if(data.numInputs<1||data.numOutputs<1||data.numSamples<=0)return kResultOk;
        auto &in=data.inputs[0];auto &out=data.outputs[0];if(in.numChannels<2||out.numChannels<2)return kResultFalse;
        if(data.symbolicSampleSize==kSample32)dsp.process(in.channelBuffers32[0],in.channelBuffers32[1],out.channelBuffers32[0],out.channelBuffers32[1],data.numSamples);
        else dsp.process(in.channelBuffers64[0],in.channelBuffers64[1],out.channelBuffers64[0],out.channelBuffers64[1],data.numSamples);
        return kResultOk;
    }
};
class Controller:public EditController{
public:
    static FUnknown* createInstance(void*){return (IEditController*)new Controller;}
    tresult PLUGIN_API initialize(FUnknown* context) override{
        auto r=EditController::initialize(context);if(r!=kResultOk)return r;
        for(int i=0;i<kParams;i++){
            char tmp[24];if(i<30)std::snprintf(tmp,sizeof(tmp),"%s%d",names[i%6],1+i/6);else std::snprintf(tmp,sizeof(tmp),"X%d",1+i-30);
            String128 title{};UString(title,128).fromAscii(tmp);
            parameters.addParameter(title,nullptr,0,i<30?(i%6==0?.625:i%6==1?.10:i%6==2?.30:i%6==3?.30:i%6==4?.5:.66666667):((i==30?160.:i==31?800.:i==32?4000.:8500.)-30)/19970.,ParameterInfo::kCanAutomate,i);
        }
        return kResultOk;
    }
    tresult PLUGIN_API setComponentState(IBStream* stream) override {IBStreamer rd(stream,kLittleEndian);for(int i=0;i<kParams;i++){float value=0;if(!rd.readFloat(value))return kResultFalse;setParamNormalized(i,normal(value));}return kResultOk;}
};
// EqualizerAPO-XT expects IPluginFactory3::setHostContext to report success.
// On Windows, Steinberg's CPluginFactory invokes registered host-context
// callbacks but returns kNotImplemented. XT currently treats that return value
// as a fatal library-loading error (AbstractLibrary::LOADING_FAILED, -2).
// Preserve the SDK callback behavior and acknowledge the supplied context.
// No host context pointer is retained: the compressor does not need one.
class XTCompatibleFactory final : public CPluginFactory {
public:
    explicit XTCompatibleFactory(const PFactoryInfo& info) : CPluginFactory(info) {}
    tresult PLUGIN_API setHostContext(FUnknown* context) override {
        const tresult result = CPluginFactory::setHostContext(context);
        return result == kNotImplemented ? kResultOk : result;
    }
};

BEGIN_FACTORY_CLASS(XTCompatibleFactory, "Infinity Audio", "https://github.com/", "mailto:info@example.invalid", PFactoryInfo::kUnicode)
DEF_CLASS2(INLINE_UID_FROM_FUID(kProcessor),PClassInfo::kManyInstances,kVstAudioEffectClass,"Infinity 5-Band Compressor",Vst::kDistributable,"Fx|Dynamics",FULL_VERSION_STR,kVstVersionString,Processor::createInstance)
DEF_CLASS2(INLINE_UID_FROM_FUID(kController),PClassInfo::kManyInstances,kVstComponentControllerClass,"Infinity 5-Band Compressor Controller",0,"",FULL_VERSION_STR,kVstVersionString,Controller::createInstance)
END_FACTORY
