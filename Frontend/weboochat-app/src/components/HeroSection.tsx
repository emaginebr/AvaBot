import { Button } from "@/components/ui/button";
import { Bot, MessageCircle, Zap } from "lucide-react";

const HeroSection = () => {
  const scrollToPlans = () => {
    const element = document.querySelector("#pricing");
    if (element) {
      element.scrollIntoView({ behavior: "smooth" });
    }
  };

  return (
    <section id="home" className="min-h-screen flex items-center justify-center relative overflow-hidden">
      {/* Background */}
      <div className="absolute inset-0 bg-gradient-hero"></div>
      <div className="absolute inset-0 bg-background/20"></div>
      
      {/* Floating elements */}
      <div className="absolute top-20 left-10 opacity-10">
        <Bot size={100} className="text-primary animate-pulse" />
      </div>
      <div className="absolute bottom-20 right-10 opacity-10">
        <MessageCircle size={80} className="text-secondary animate-pulse" />
      </div>
      <div className="absolute top-1/2 right-1/4 opacity-5">
        <Zap size={120} className="text-primary-glow" />
      </div>

      {/* Content */}
      <div className="relative z-10 text-center max-w-4xl mx-auto px-4">
        <div className="mb-8">
          <h1 className="text-5xl md:text-7xl font-bold text-foreground mb-6 leading-tight">
            Automate your
            <span className="bg-gradient-primary bg-clip-text text-transparent"> customer service</span>
            <br />
            with intelligence
          </h1>
          <p className="text-xl md:text-2xl text-muted-foreground max-w-3xl mx-auto leading-relaxed">
            Transform your customer service experience with our platform integrated with 
            <span className="text-primary font-semibold"> ChatGPT</span> and 
            <span className="text-secondary font-semibold"> WhatsApp</span>
          </p>
        </div>

        <div className="flex flex-col sm:flex-row gap-4 justify-center items-center">
          <Button 
            variant="hero" 
            size="lg" 
            className="text-lg px-8 py-4 h-14"
            onClick={scrollToPlans}
          >
            <Zap className="w-5 h-5 mr-2" />
            Get Started
          </Button>
          <Button 
            variant="glass" 
            size="lg" 
            className="text-lg px-8 py-4 h-14"
            onClick={() => {
              const element = document.querySelector("#features");
              if (element) element.scrollIntoView({ behavior: "smooth" });
            }}
          >
            Learn More
          </Button>
        </div>

        {/* Stats */}
        <div className="mt-16 grid grid-cols-1 md:grid-cols-3 gap-8 max-w-2xl mx-auto">
          <div className="text-center">
            <div className="text-3xl font-bold text-primary mb-2">24/7</div>
            <div className="text-muted-foreground">Support</div>
          </div>
          <div className="text-center">
            <div className="text-3xl font-bold text-secondary mb-2">95%</div>
            <div className="text-muted-foreground">Satisfaction</div>
          </div>
          <div className="text-center">
            <div className="text-3xl font-bold text-primary-glow mb-2">10x</div>
            <div className="text-muted-foreground">Faster</div>
          </div>
        </div>
      </div>

      {/* Scroll indicator */}
      <div className="absolute bottom-8 left-1/2 transform -translate-x-1/2 animate-bounce">
        <div className="w-6 h-10 border-2 border-primary/30 rounded-full flex justify-center">
          <div className="w-1 h-3 bg-primary rounded-full mt-2 animate-pulse"></div>
        </div>
      </div>
    </section>
  );
};

export default HeroSection;